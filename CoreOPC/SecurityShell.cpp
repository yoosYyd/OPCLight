#include "SecurityShell.h"

void SecurityShell::AddSession(unsigned long session, char* user)
{
	this->usersSes[session] = std::string(user);
}
void SecurityShell::RemoveSession(unsigned long session)
{
	UA_LOG_INFO(UA_Log_Stdout, UA_LOGCATEGORY_USERLAND, "disconnected: %s", usersSes[session].c_str());
	this->usersSes.erase(session);
}
UA_UsernamePasswordLogin* SecurityShell::GetUsersData(int& userCount)
{
	return this->LoadUsers(userCount);
}
int SecurityShell::LookAccessLvl(unsigned long session)
{
	return this->LookUserAccessLvl(usersSes[session]);
}
int SecurityShell::GetActiveUsersCount()
{
	return this->usersSes.size();
}