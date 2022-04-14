#include "logger.h"
#include <codecvt>
#pragma warning(disable : 4996)

Logger& Logger::GetI()
{
	static Logger instance;
	return instance;
}
void Logger::SendMessage(std::string msg)
{
	if (s == -1)
	{
		//printf("%s %d\n", msg.c_str(), s);
		InitSocket();
	}
	char timeStr[64] = "";
	time_t current = time(0);
	tm* lt = localtime(&current);
	sprintf(timeStr, "%d-%02d-%02d %02d:%02d:%02d ",
		lt->tm_year+1900, lt->tm_mon+1, lt->tm_mday, lt->tm_hour, lt->tm_min, lt->tm_sec);
	std::string message = std::string(timeStr) + " {" + MARKER_TAG + "} " + msg + "\r\n";
#ifdef _MSC_VER 
	sendto(s, message.c_str(), message.size(), 0, (SOCKADDR*)&targAddr, sizeof(targAddr));
#else
	sendto(s, message.c_str(), message.size(), 0, (struct sockaddr*)&targAddr, sizeof(targAddr));
#endif
}
std::string Logger::format(const char* fmt, ...)
{
	va_list args;
	va_start(args, fmt);
	std::vector<char> v(4096);
	while (true)
	{
		va_list args2;
		va_copy(args2, args);
		int res = vsnprintf(v.data(), v.size(), fmt, args2);
		if ((res >= 0) && (res < static_cast<int>(v.size())))
		{
			va_end(args);
			va_end(args2);
			return std::string(v.data());
		}
		size_t size;
		if (res < 0)
			size = v.size() * 2;
		else
			size = static_cast<size_t>(res) + 1;
		v.clear();
		v.resize(size);
		va_end(args2);
	}
}
void Logger::InitSocket()
{
#ifdef _MSC_VER 
	WSADATA wsd = { 0 };
	WSAStartup(MAKEWORD(2, 2), &wsd);
#endif		
	s = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
	targAddr.sin_family = AF_INET;
	targAddr.sin_port = htons(PORT);
	inet_pton(AF_INET, LOG_SERV, &targAddr.sin_addr);
}
