using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

namespace MDBFeeder.TOOLS
{
    public class OPCUAtool
    {
        private UaTcpSessionChannel channel;
        private ApplicationDescription clientDescription = new ApplicationDescription
        {
            ApplicationName = "UaClient.S7Communicator",
            ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:UaClient.S7Communicator",
            ApplicationType = ApplicationType.Client
        };
        private UserNameIdentity authID /*= new UserNameIdentity("User", "111")*/;

        public OPCUAtool(string user,string pass)
        {
            authID = new UserNameIdentity(user, pass);
        }
        public bool IsConnected()
        {
            if (channel != null)
            {
                return channel.State == CommunicationState.Opened;
            }
            return false;
        }
        public void Connect()
        {
            channel = new UaTcpSessionChannel(
            clientDescription,
            null, // no x509 certificates
            authID,
            "opc.tcp://127.0.0.1:4888/",
            SecurityPolicyUris.None);
            try
            {
                channel.Faulted += Event_Faulted;
                channel.Opened += Event_Opened;
                channel.OpenAsync().Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine("InitOPCConn error: " + ex.Message);
                HumbleLogger.Instance.ErrorMessage("InitOPCConn error: " + ex.Message);
                //channel.AbortAsync().Wait();
            }
        }
        private void Event_Faulted(object sender, EventArgs e)
        {
            try
            {
                channel.AbortAsync().Wait();
            }
            catch (Exception ex)
            {
                HumbleLogger.Instance.ErrorMessage("OPCUA aborting conn error: " + ex.Message);
                Console.WriteLine("aborting conn error: " + ex.Message);
            }
            channel = null;
            Console.WriteLine("connection faulted");
            HumbleLogger.Instance.DebugMessage("OPCUA connection faulted");
            Thread.Sleep(10000);
            Connect();
        }
        private void Event_Opened(object sender, EventArgs e)
        {
            Console.WriteLine("connection opened,state: " + channel.State.ToString());
            HumbleLogger.Instance.DebugMessage("OPCUA connection opened,state: " + channel.State.ToString());
        }
        public void Disconnect()
        {
            if (channel.State == Workstation.ServiceModel.Ua.CommunicationState.Opened)
            {
                channel.CloseAsync().Wait();
            }
        }
        public object ReadTAG(string tagName)
        {
            var readRequest = new ReadRequest
            {
                NodesToRead = new[] {
                    new ReadValueId {
                        NodeId = NodeId.Parse("ns=1;s="+tagName),
                        AttributeId = AttributeIds.Value
                    }
                }
            };
            var task = channel.ReadAsync(readRequest);
            try
            {
                task.Wait();
            }
            catch (Exception er)
            {
                Console.WriteLine("Tag reading exception: {0}", er.GetBaseException().Message);
                HumbleLogger.Instance.ErrorMessage("OPCUA Tag reading exception: " + er.GetBaseException().Message);
                return null;
            }
            return task.Result.Results[0].Value;
        }
        public void WriteTAG(string tagName,DataValue data)
        {
            var writeRequest = new WriteRequest
            {
                NodesToWrite = new[] {
                    new WriteValue {
                        NodeId = NodeId.Parse("ns=1;s="+tagName),
                        AttributeId = AttributeIds.Value,
                        Value = data
                    }
                }
            };
            try
            {
                channel.WriteAsync(writeRequest).Wait();
            }
            catch (Exception er)
            {
                Console.WriteLine("Tag write exception: {0}", er.GetBaseException().Message);
                HumbleLogger.Instance.ErrorMessage("OPCUA Tag write exception: " + er.GetBaseException().Message);
            }
        }
    }
}
