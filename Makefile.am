AUTOMAKE_OPTIONS = foreign subdir-objects

# Make these come from pkg-config or autoconf dependancy detection later
COREHOST_CFLAGS = -I/usr/share/dotnet/packs/Microsoft.NETCore.App.Host.linux-x64/3.1.9/runtimes/linux-x64/native
COREHOST_LDFLAGS = -L/usr/share/dotnet/packs/Microsoft.NETCore.App.Host.linux-x64/3.1.9/runtimes/linux-x64/native -lnethost

mod_LTLIBRARIES = mod_coreclr.la
mod_coreclr_la_SOURCES  = src/mod_coreclr.c src/swig.cpp src/ManagedSession.cpp
mod_coreclr_la_CFLAGS = -I./inc $(FREESWITCH_CFLAGS) -Wall $(COREHOST_CFLAGS)
mod_coreclr_la_CXXFLAGS = -I./inc $(FREESWITCH_CFLAGS) -Wall $(COREHOST_CFLAGS)
mod_coreclr_la_LDFLAGS  = -avoid-version -module -no-undefined -shared $(FREESWITCH_LDFLAGS) $(COREHOST_LDFLAGS)
