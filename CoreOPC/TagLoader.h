#pragma once
#include "ConfigParser.h"
class TagLoader :public ConfigParser
{
private:
	UA_Server* server;
	UA_NodeId AddRoot(char* rootID, UA_NodeId parent = UA_NODEID_NULL);
	void InsertTag(UA_NodeId root, const char* name, const char* type, const char* accesType, void* initValue);
	std::map<std::string, unsigned char> accesMap;
public:
#ifdef _MSC_VER 
	TagLoader(const wchar_t* config, UA_Server* server) :ConfigParser(config)
	{
		this->server = server;
	}
#endif
	TagLoader(const char* config, UA_Server* server) :ConfigParser(config)
	{
		this->server = server;
	}
	~TagLoader() {}
	void Load();
	bool IsTagReadOnly(std::string tagID);
};

