// CoreOPC.cpp : Этот файл содержит функцию "main". Здесь начинается и заканчивается выполнение программы.
//
#ifdef _MSC_VER 
#pragma comment(lib, "Ws2_32.lib")
#pragma comment(lib, "Shlwapi.lib")
#pragma comment(lib, "Shell32.lib")
#pragma comment(lib, "Bcrypt.lib")
#pragma once
#pragma warning(disable: 4996)

#include <shlwapi.h>
#include <shlobj_core.h>
#include <conio.h>
#endif

//#include "open62541/open62541.h"
#include "json.hpp"
#include "SecurityShell.h"
#include "TagLoader.h"
#include "logger.h"

#include <iostream>
#include <signal.h>
#include <filesystem>

UA_Boolean running = true;
UA_StatusCode retval;
UA_Server* server = NULL;
TagLoader* loader = NULL;
/*************************************/
#ifdef _MSC_VER 
HANDLE serviceThr = 0;
SERVICE_STATUS serviceStatus;
SERVICE_STATUS_HANDLE serviceStatusHandle;
wchar_t serviceName[] = L"OPC_core";
void ServiceMain(int argc, char** argv);
#endif
/************************************/
#ifdef _MSC_VER 
std::wstring GetCurrentPatch()
{
	wchar_t selfdir[MAX_PATH] = { 0 };
	GetModuleFileNameW(NULL, selfdir, MAX_PATH);
	PathRemoveFileSpecW(selfdir);
	return std::wstring(selfdir);
}
#else
std::string GetCurrentPatch()
{
	return std::string(getcwd(0, 0));
}
#endif
/*GCC building instruction: /usr/bin/aarch64-linux-gnu-g++ -fpermissive -o opc *.cpp ./open62541/open62541.c */
SecurityShell* ssh;
static UA_StatusCode ActivateSession(UA_Server* server, UA_AccessControl* ac,
	const UA_EndpointDescription* endpointDescription,
	const UA_ByteString* secureChannelRemoteCertificate,
	const UA_NodeId* sessionId,
	const UA_ExtensionObject* userIdentityToken,
	void** sessionContext)
{
	const UA_UserNameIdentityToken* userToken =
		(UA_UserNameIdentityToken*)userIdentityToken->content.decoded.data;
	AccessControlContext* context = (AccessControlContext*)ac->context;
	if (userToken->userName.length == 0 && userToken->password.length == 0)
	{
		return UA_STATUSCODE_BADIDENTITYTOKENINVALID;
	}
	for (size_t i = 0; i < context->usernamePasswordLoginSize; i++)
	{
		if (UA_String_equal(&userToken->userName, &context->usernamePasswordLogin[i].username) &&
			UA_String_equal(&userToken->password, &context->usernamePasswordLogin[i].password))
		{
			char buf[256] = "";
			if (userToken->userName.length > 256)
			{
				return UA_STATUSCODE_BADIDENTITYTOKENINVALID;
			}
			memcpy(buf, userToken->userName.data, userToken->userName.length);
			if (ssh != NULL)
			{
				UA_LOG_INFO(UA_Log_Stdout, UA_LOGCATEGORY_USERLAND, "connected: %s", buf);
				ssh->AddSession((unsigned long)sessionId, buf);
				UA_LOG_INFO(UA_Log_Stdout, UA_LOGCATEGORY_USERLAND,
					"active users on server: %d", ssh->GetActiveUsersCount());
			}
			return UA_STATUSCODE_GOOD;
		}
	}
	return UA_STATUSCODE_BADIDENTITYTOKENINVALID;
}
UA_Byte GetUserAccessLevel(UA_Server* server, UA_AccessControl* ac,
	const UA_NodeId* sessionId, void* sessionContext,
	const UA_NodeId* nodeId, void* nodeContext)
{
	//printf("!!!! %s %d\n", nodeId->identifier.string.data, nodeId->identifier.numeric);
	if (ssh == NULL)
	{
		return 0;
	}
	if (loader != NULL && nodeId->identifier.string.data !=NULL)
	{
		if (ssh->LookAccessLvl((unsigned long)sessionId) == 0)
		{
			return 0xFF;
		}
		//printf("!!!!! %s\n", nodeId->identifier.string.data);
		std::string tID = std::string((char*)nodeId->identifier.string.data, nodeId->identifier.string.length);
		if (loader->IsTagReadOnly(tID))
		{
			return UA_ACCESSLEVELMASK_READ;
		}
		else
		{
			return UA_ACCESSLEVELMASK_READ | UA_ACCESSLEVELMASK_WRITE;
		}
	}
	return 0;
	//return UA_ACCESSLEVELMASK_READ | UA_ACCESSLEVELMASK_WRITE;
}
void CloseSession(UA_Server* server, UA_AccessControl* ac,
	const UA_NodeId* sessionId, void* sessionContext)
{
	ssh->RemoveSession((unsigned long)sessionId);
	UA_LOG_INFO(UA_Log_Stdout, UA_LOGCATEGORY_USERLAND,
		"active users on server: %d", ssh->GetActiveUsersCount());
}
/*****************************************************************************************/
void MyUALogger(void* logContext, UA_LogLevel level, UA_LogCategory category,
	const char* msg, va_list args)
{
	vprintf(msg, args);
	printf("\n");
	//fflush(stdout);
	char buf[4096] = "";
	vsprintf(buf, msg, args);
	Logger& log = Logger::GetI();
	log.SendMessage(std::string(buf));
}

void StartOPC()
{
#ifdef _MSC_VER 
	ssh = new SecurityShell((GetCurrentPatch() + L"\\config.json").c_str());
#else
	ssh = new SecurityShell((GetCurrentPatch() + "/config.json").c_str());
#endif
	server = UA_Server_new();
	UA_ServerConfig* config = UA_Server_getConfig(server);
	UA_ServerConfig_setMinimal(config, 4888, NULL);
	int usersCount = 0;
	UA_UsernamePasswordLogin* users = ssh->GetUsersData(usersCount);
	retval = UA_AccessControl_default(config, false,
		&config->securityPolicies[config->securityPoliciesSize - 1].policyUri, usersCount, users);
	config->accessControl.getUserAccessLevel = GetUserAccessLevel;
	config->accessControl.activateSession = ActivateSession;
	config->accessControl.closeSession = CloseSession;
	//config->accessControl.getUserRightsMask = GetUserRightsMask;
	config->logger.log = MyUALogger;
#ifdef _MSC_VER 	
	loader = new TagLoader((GetCurrentPatch() + L"\\config.json").c_str(), server);
#else
	loader = new TagLoader((GetCurrentPatch() + "/config.json").c_str(), server);
#endif
	loader->Load();
	//loader->~TagLoader();
	retval = UA_Server_run(server, &running);
	UA_Server_delete(server);
}

int main(int argc, char* argv[])
{
	//StartOPC();
#ifdef _MSC_VER 
	if (argc > 1)
	{
		StartOPC();
		_getch();
	}
	else
	{
		SERVICE_TABLE_ENTRY ServiceTable[1];
		ServiceTable[0].lpServiceName = serviceName;
		ServiceTable[0].lpServiceProc = (LPSERVICE_MAIN_FUNCTION)ServiceMain;
		StartServiceCtrlDispatcher(ServiceTable);
	}
#else
	StartOPC();
#endif
}

#ifdef _MSC_VER 
void ControlHandler(DWORD request)
{
	switch (request)
	{
	case SERVICE_CONTROL_STOP:
	{
		serviceStatus.dwWin32ExitCode = 0;
		serviceStatus.dwCurrentState = SERVICE_STOPPED;
		SetServiceStatus(serviceStatusHandle, &serviceStatus);
	}break;
	case SERVICE_CONTROL_SHUTDOWN:
	{
		serviceStatus.dwWin32ExitCode = 0;
		serviceStatus.dwCurrentState = SERVICE_STOPPED;
		SetServiceStatus(serviceStatusHandle, &serviceStatus);
	}break;
	default:
		break;
	}
}
DWORD WINAPI StartOPCServiceThread(LPVOID lParam)
{
	StartOPC();
	return 0;
}
void ServiceMain(int argc, char** argv)
{
	serviceStatus.dwServiceType = SERVICE_WIN32_OWN_PROCESS;
	serviceStatus.dwCurrentState = SERVICE_START_PENDING;
	serviceStatus.dwControlsAccepted = SERVICE_ACCEPT_STOP | SERVICE_ACCEPT_SHUTDOWN;
	serviceStatus.dwWin32ExitCode = 0;
	serviceStatus.dwServiceSpecificExitCode = 0;
	serviceStatus.dwCheckPoint = 0;
	serviceStatus.dwWaitHint = 0;

	serviceStatusHandle = RegisterServiceCtrlHandler(serviceName, (LPHANDLER_FUNCTION)ControlHandler);
	if (serviceStatusHandle == (SERVICE_STATUS_HANDLE)0) {
		return;
	}

	serviceStatus.dwCurrentState = SERVICE_RUNNING;
	SetServiceStatus(serviceStatusHandle, &serviceStatus);
	serviceThr = CreateThread(0, 0, StartOPCServiceThread, 0, 0, 0);
	while (serviceStatus.dwCurrentState == SERVICE_RUNNING)
	{
		Sleep(100);
	}
	TerminateThread(serviceThr, 0);
	return;
}
#endif

