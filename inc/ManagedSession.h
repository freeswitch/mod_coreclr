#ifndef MANAGED_SESSION_H
#define MANAGED_SESSION_H

extern "C" {
#include <switch.h>
#include <switch_cpp.h>
}
typedef char* (*inputFunction) (char *);

class ManagedSession : public CoreSession
{
public:
	ManagedSession(void);
	ManagedSession(char *uuid);
	ManagedSession(switch_core_session_t *session);
	virtual ~ManagedSession();

    virtual bool begin_allow_threads();
    virtual bool end_allow_threads();
    virtual void check_hangup_hook();
    virtual switch_status_t run_dtmf_callback(void *input, switch_input_type_t itype);
};

#endif
