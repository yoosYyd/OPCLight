#include "TagLoader.h"

UA_NodeId TagLoader::AddRoot(char* rootID, UA_NodeId parent)
{
	UA_NodeId ret;
	UA_ObjectAttributes oAttr = UA_ObjectAttributes_default;
	oAttr.displayName = UA_LOCALIZEDTEXT((char*)"en-US", rootID);
	if (memcmp(&parent, &UA_NODEID_NULL, sizeof(UA_NodeId)) == 0)
	{
		UA_Server_addObjectNode(server, UA_NODEID_NULL,
			UA_NODEID_NUMERIC(0, UA_NS0ID_OBJECTSFOLDER),
			UA_NODEID_NUMERIC(0, UA_NS0ID_ORGANIZES),
			UA_QUALIFIEDNAME(1, rootID), UA_NODEID_NUMERIC(0, UA_NS0ID_BASEOBJECTTYPE),
			oAttr, NULL, &ret);
	}
	else
	{
		UA_Server_addObjectNode(server, UA_NODEID_NULL,
			parent,
			UA_NODEID_NUMERIC(0, UA_NS0ID_ORGANIZES),
			UA_QUALIFIEDNAME(1, rootID), UA_NODEID_NUMERIC(0, UA_NS0ID_BASEOBJECTTYPE),
			oAttr, NULL, &ret);
	}
	return ret;
}
bool TagLoader::IsTagReadOnly(std::string tagID)
{
	if (this->accesMap.size() > 0)
	{
		//printf("key %s %d\n", tagID.c_str(), this->accesMap.count(tagID));
		if (this->accesMap.count(tagID) > 0)
		{
			//printf("acess %d\n", this->accesMap[tagID]);
			return this->accesMap[tagID] == 0;
		}
	}
	return true;
}
void TagLoader::InsertTag(UA_NodeId root, const char* name, const char* type, const char* accesType, void* initValue)
{
	unsigned char access = 0xFF;
	if (strcmp(accesType, "R") == 0)
	{
		access = 0;
	}
	if (strcmp(accesType, "RW") == 0)
	{
		access = 1;
	}
	this->accesMap[std::string(name)] = access;
	//printf("%s %s %d %d\n", name, accesType, access, this->accesMap.size());
	UA_VariableAttributes attr = UA_VariableAttributes_default;
	if (strcmp(type, "uint8") == 0)
	{
		UA_Byte uauint = *(UA_Byte*)initValue;
		UA_Variant_setScalarCopy(&attr.value, &uauint, &UA_TYPES[UA_TYPES_BYTE]);
		attr.dataType = UA_TYPES[UA_TYPES_BYTE].typeId;
	}
	if (strcmp(type, "uint16") == 0)
	{
		UA_UInt16 uauint = *(UA_UInt16*)initValue;
		UA_Variant_setScalarCopy(&attr.value, &uauint, &UA_TYPES[UA_TYPES_UINT16]);
		attr.dataType = UA_TYPES[UA_TYPES_UINT16].typeId;
	}
	if (strcmp(type, "uint32") == 0)
	{
		UA_UInt32 uauint = *(UA_UInt32*)initValue;
		UA_Variant_setScalarCopy(&attr.value, &uauint, &UA_TYPES[UA_TYPES_UINT32]);
		attr.dataType = UA_TYPES[UA_TYPES_UINT32].typeId;
	}
	if (strcmp(type, "uint64") == 0)
	{
		UA_UInt64 uauint = *(UA_UInt64*)initValue;
		UA_Variant_setScalarCopy(&attr.value, &uauint, &UA_TYPES[UA_TYPES_UINT64]);
		attr.dataType = UA_TYPES[UA_TYPES_UINT64].typeId;
	}
	if (strcmp(type, "int8") == 0)
	{
		UA_Byte uauint = *(UA_Byte*)initValue;
		UA_Variant_setScalarCopy(&attr.value, &uauint, &UA_TYPES[UA_TYPES_SBYTE]);
		attr.dataType = UA_TYPES[UA_TYPES_SBYTE].typeId;
	}
	if (strcmp(type, "int16") == 0)
	{
		UA_UInt16 uauint = *(UA_UInt16*)initValue;
		UA_Variant_setScalarCopy(&attr.value, &uauint, &UA_TYPES[UA_TYPES_INT16]);
		attr.dataType = UA_TYPES[UA_TYPES_INT16].typeId;
	}
	if (strcmp(type, "int32") == 0)
	{
		UA_UInt32 uauint = *(UA_UInt32*)initValue;
		UA_Variant_setScalarCopy(&attr.value, &uauint, &UA_TYPES[UA_TYPES_INT32]);
		attr.dataType = UA_TYPES[UA_TYPES_INT32].typeId;
	}
	if (strcmp(type, "int64") == 0)
	{
		UA_UInt64 uauint = *(UA_UInt64*)initValue;
		UA_Variant_setScalarCopy(&attr.value, &uauint, &UA_TYPES[UA_TYPES_INT64]);
		attr.dataType = UA_TYPES[UA_TYPES_INT64].typeId;
	}
	if (strcmp(type, "float32") == 0)
	{
		UA_Float uafloat = *(UA_Float*)initValue;
		UA_Variant_setScalarCopy(&attr.value, &uafloat, &UA_TYPES[UA_TYPES_FLOAT]);
		attr.dataType = UA_TYPES[UA_TYPES_FLOAT].typeId;
	}
	if (strcmp(type, "float64") == 0)
	{
		UA_Double uafloat = *(UA_Double*)initValue;
		UA_Variant_setScalarCopy(&attr.value, &uafloat, &UA_TYPES[UA_TYPES_DOUBLE]);
		attr.dataType = UA_TYPES[UA_TYPES_DOUBLE].typeId;
	}
	attr.accessLevel = UA_ACCESSLEVELMASK_READ | UA_ACCESSLEVELMASK_WRITE;
	UA_NodeId myIntegerNodeId = UA_NODEID_STRING_ALLOC(1, name);
	UA_QualifiedName myIntegerName = UA_QUALIFIEDNAME_ALLOC(1, name);
	UA_Server_addVariableNode(server, myIntegerNodeId, root,
		UA_NODEID_NUMERIC(0, UA_NS0ID_ORGANIZES), myIntegerName,
		UA_NODEID_NUMERIC(0, UA_NS0ID_BASEDATAVARIABLETYPE), attr, NULL, NULL);
	UA_VariableAttributes_clear(&attr);
	UA_NodeId_clear(&myIntegerNodeId);
	UA_QualifiedName_clear(&myIntegerName);
}

void TagLoader::Load()
{
	json data = this->GetFeeders();
	json tags;
	json tagsGroup;
	UA_NodeId root, subRoot;
	std::string tagFullName;
	unsigned int initVal = 0;

	for (const auto& item : data.items())
	{
		root = AddRoot((char*)item.key().c_str());
		tagFullName = item.key() + ".";
		tags = data[item.key()]["TAGS"];
		for (const auto& group : tags.items())
		{
			tagsGroup = tags[group.key()];
			subRoot = AddRoot((char*)group.key().c_str(), root);
			tagFullName = item.key() + "." + group.key() + ".";
			for (const auto& tag : tagsGroup.items())
			{
				initVal = tag.value()["InitValue"].get<unsigned int>();
				InsertTag(subRoot, (tagFullName + tag.key()).c_str(), tag.value()["type"].get<std::string>().c_str(),
					tag.value()["Acesses"].get<std::string>().c_str(), &initVal);
			}
		}
	}
}
