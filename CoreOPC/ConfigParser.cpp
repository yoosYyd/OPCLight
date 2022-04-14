#include "ConfigParser.h"
#include <iostream>

#ifdef _MSC_VER 
ConfigParser::ConfigParser(const wchar_t* configSite)
{
	std::ifstream ifs(configSite); 
	try
	{
		rootDocument = json::parse(ifs);
	}
	catch (std::exception& e)
	{
		std::cerr << e.what() << std::endl;
	}
	ifs.close();
	this->users = rootDocument["USERS"];
	this->tagFeeders = rootDocument["FEEDERS"];
}
#endif

ConfigParser::ConfigParser(const char* configSite)
{
	std::ifstream ifs(configSite);
	try
	{
		rootDocument = json::parse(ifs);
	}
	catch (std::exception& e)
	{
		std::cerr << e.what() << std::endl;
	}
	ifs.close();
	this->users = rootDocument["USERS"];
	this->tagFeeders = rootDocument["FEEDERS"];
}

UA_UsernamePasswordLogin* ConfigParser::LoadUsers(int& usersCount)
{
	UA_UsernamePasswordLogin* ret = NULL;
	ret = new UA_UsernamePasswordLogin[users.size()];
	usersCount = users.size();
	int retCount = 0;
	for (const auto& item : users.items())
	{
		ret[retCount].username = UA_STRING_ALLOC(item.key().c_str());
		ret[retCount].password = UA_STRING_ALLOC(item.value()["pass"].get<std::string>().c_str());
		retCount++;
	}
	return ret;
}
int ConfigParser::LookUserAccessLvl(std::string userName)
{
	for (const auto& item : users.items())
	{
		if (userName.compare(item.key()) == 0)
		{
			//return atoi(item.value()["accesslvl"].get<std::string>().c_str());
			return item.value()["accesslvl"].get <int>();
		}
	}
	return -1;
}
json ConfigParser::GetFeeders()
{
	return this->tagFeeders;
}
