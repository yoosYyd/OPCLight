using ArchiverII.SHARED;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

namespace ArchiverII.CODE
{
    public class OPCwatcher
    {
        private string OPCuser = "";
        private string OPCpass = "";
        private List<TAG> watchedList;
        private UaTcpSessionChannel channel;
        private ApplicationDescription clientDescription = new ApplicationDescription
        {
            ApplicationName = "UaClient.S7Communicator",
            ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:UaClient.S7Communicator",
            ApplicationType = ApplicationType.Client
        };
        public OPCwatcher(string OPCuser,string OPCpass, List<TAG> tags)
        {
            this.OPCuser = OPCuser;
            this.OPCpass = OPCpass;
            watchedList = tags;
            //authID = new UserNameIdentity(OPCuser, OPCpass);
        }
        public void Connect()
        {
            channel = new UaTcpSessionChannel(
            clientDescription,
            null, // no x509 certificates
            new UserNameIdentity(OPCuser, OPCpass),
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
            Thread.Sleep(1000);
            Connect();
        }
        private void Event_Opened(object sender, EventArgs e)
        {
            Console.WriteLine("connection opened,state: " + channel.State.ToString());
            HumbleLogger.Instance.DebugMessage("OPCUA connection opened,state: " + channel.State.ToString());
        }
        private void Disconnect()
        {
            if (channel.State == Workstation.ServiceModel.Ua.CommunicationState.Opened)
            {
                channel.CloseAsync().Wait();
            }
        }
        private object ReadTAG(string tagName)
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
        public void Watching(SQLtool st)
        {
            Connect();
            for (; ; )
            {
                foreach (TAG tag in watchedList)
                {
                    switch (tag.OPCtype)
                    {
                        case "uint16":
                            {
                                tag.value = ((ushort)ReadTAG(tag.ID)).ToString(CultureInfo.InvariantCulture);
                            }
                            break;
                        case "int16":
                            {
                                tag.value = ((short)ReadTAG(tag.ID)).ToString(CultureInfo.InvariantCulture);
                            }
                            break;
                        case "uint32":
                            {
                                tag.value = ((uint)ReadTAG(tag.ID)).ToString(CultureInfo.InvariantCulture);
                            }
                            break;
                        case "int32":
                            {
                                tag.value = ((int)ReadTAG(tag.ID)).ToString(CultureInfo.InvariantCulture);
                            }
                            break;
                        case "uint64":
                            {
                                tag.value = ((ulong)ReadTAG(tag.ID)).ToString(CultureInfo.InvariantCulture);
                            }
                            break;
                        case "int64":
                            {
                                tag.value = ((long)ReadTAG(tag.ID)).ToString(CultureInfo.InvariantCulture);
                            }
                            break;
                        case "float32":
                            {
                                tag.value = ((float)ReadTAG(tag.ID)).ToString(CultureInfo.InvariantCulture);
                            }
                            break;
                        case "float64":
                            {
                                tag.value = ((double)ReadTAG(tag.ID)).ToString(CultureInfo.InvariantCulture);
                            }
                            break;
                    }
                    //Console.WriteLine(tag.value);
                }
                st.RunLogging(DateTime.Now);
                Thread.Sleep(100);
            }
        }
    }
}
