#pragma once
#include <fstream>
#include "open62541/open62541.h"
#include "json.hpp"

using json = nlohmann::json;

class ConfigParser
{
private:
	json rootDocument;
	json users;
	json tagFeeders;
protected:
	UA_UsernamePasswordLogin* LoadUsers(int& usersCount);
	int LookUserAccessLvl(std::string userName);
public:
#ifdef _MSC_VER 
	ConfigParser(const wchar_t* configSite);
#endif
	ConfigParser(const char* configSite);
	~ConfigParser() {}
	json GetFeeders();
};

