#ifdef _MSC_VER 
#pragma once
#include <WS2tcpip.h>
#include <WinSock2.h>
#include <windows.h>
#else
#include <iostream>
#include <stdint.h>
#include <sys/socket.h>
#include <arpa/inet.h>
#include <stdarg.h>
#endif
#include <vector>
#include <string>

#ifdef _MSC_VER 
typedef SOCKET SOCK_;
#else
typedef int SOCK_;
//#define IPPROTO_UDP 0
#endif

#define PORT 45454
#define LOG_SERV "127.0.0.1"
#define MARKER_TAG "OPC_CORE"

class Logger
{
private:
	SOCK_ s=-1;
	sockaddr_in targAddr={};
	void InitSocket();
protected:
	Logger() {}
	Logger(const Logger&);
	void operator=(Logger const&);
public:
	static Logger& GetI();
	void SendMessage(std::string msg);
	std::string format(const char* fmt, ...);
	~Logger() {}
};
