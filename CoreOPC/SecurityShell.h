#pragma once
#include <map>
#include "ConfigParser.h"

typedef struct {
	UA_Boolean allowAnonymous;
	size_t usernamePasswordLoginSize;
	UA_UsernamePasswordLogin* usernamePasswordLogin;
} AccessControlContext;

class SecurityShell :public ConfigParser
{
private:
	json document;
	std::map <unsigned long, std::string> usersSes;
public:
#ifdef _MSC_VER 
	SecurityShell(const wchar_t* usersConfig) :ConfigParser(usersConfig){}
#endif
	SecurityShell(const char* usersConfig) :ConfigParser(usersConfig) {}
	~SecurityShell(){}
	UA_UsernamePasswordLogin* GetUsersData(int& userCount);
	void AddSession(unsigned long session, char* user);
	void RemoveSession(unsigned long session);
	int LookAccessLvl(unsigned long session);
	int GetActiveUsersCount();
};

