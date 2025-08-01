#pragma once

#ifdef _WIN32
#include <windows.h>
EXTERN_C IMAGE_DOS_HEADER __ImageBase;
#define HINST_THISCOMPONENT ((HINSTANCE) &__ImageBase)
#else
#define _GNU_SOURCE
#include <dlfcn.h>
#endif

#include "../../../shared/src/native-src/dd_filesystem.hpp"
// namespace fs is an alias defined in "dd_filesystem.hpp"
#include "../../../shared/src/native-src/pal.h"
#include "../../../shared/src/native-src/string.h"
#include "./log.h"

const std::string conf_filename = "loader.conf";
const ::shared::WSTRING cfg_filepath_env = WStr("DD_NATIVELOADER_CONFIGFILE");
const ::shared::WSTRING cfg_instrumentation_verification_env = WStr("DD_WRITE_INSTRUMENTATION_TO_DISK");
const ::shared::WSTRING cfg_copying_originals_modules_env = WStr("DD_COPY_ORIGINALS_MODULES_TO_DISK");
const ::shared::WSTRING cfg_log_directory_env = WStr("DD_TRACE_LOG_DIRECTORY");

// Note that this list should be kept in sync with the values in tracer/src/Datadog.Tracer.Native/dd_profiler_constants.h
// Note that you should also consider adding to the SSI tracer/build/artifacts/requirements.json file
// FIXME: this should also take into account case insensitivity, but that is not yet supported
// https://devblogs.microsoft.com/oldnewthing/20241007-00/?p=110345
const shared::WSTRING default_exclude_assemblies[]{
    WStr("aspnet_compiler.exe"),
    WStr("aspnet_state.exe"),
    WStr("CollectGuestLogs.exe"), // https://github.com/Azure/WindowsVMAgent
    WStr("csc.exe"),
    WStr("dd-trace"),
    WStr("dd-trace.exe"),
    WStr("devenv.exe"),
    WStr("iisexpresstray.exe"),
    WStr("InetMgr.exe"),
    WStr("Microsoft.ServiceHub.Controller.exe"),
    WStr("MSBuild.exe"),
    WStr("MsDtsSrvr.exe"),
    WStr("msvsmon.exe"),
    WStr("PerfWatson2.exe"),
    WStr("powershell.exe"),
    WStr("pwsh.exe"),
    WStr("pwsh"),
    WStr("ServiceHub.DataWarehouseHost.exe"),
    WStr("ServiceHub.Host.CLR.exe"),
    WStr("ServiceHub.Host.CLR.x86.exe"),
    WStr("ServiceHub.IdentityHost.exe"),
    WStr("ServiceHub.RoslynCodeAnalysisService32.exe"),
    WStr("ServiceHub.SettingsHost.exe"),
    WStr("ServiceHub.TestWindowStoreHost.exe"),
    WStr("ServiceHub.ThreadedWaitDialog.exe"),
    WStr("ServiceHub.VSDetouredHost.exe"),
    WStr("sqlagent.exe"),
    WStr("sqlbrowser.exe"),
    WStr("sqlservr.exe"),
    WStr("VBCSCompiler.exe"),
    WStr("vsdbg"),
    WStr("vsdbg.exe"),
    WStr("WaAppAgent.exe"),            // https://github.com/Azure/WindowsVMAgent
    WStr("WerFault.exe"),              // WER = Windows Error Reporting - can kick in when a process crashes
    WStr("WindowsAzureGuestAgent.exe") // https://github.com/Azure/WindowsVMAgent
};

inline static const ::shared::WSTRING datadog_logs_folder_path = WStr(R"(Datadog .NET Tracer\logs)");

static fs::path GetCurrentModuleFolderPath()
{
#ifdef _WIN32
    WCHAR moduleFilePath[MAX_PATH] = {0};
    if (::GetModuleFileName((HMODULE) HINST_THISCOMPONENT, moduleFilePath, MAX_PATH) != 0)
    {
        return fs::path(moduleFilePath).remove_filename();
    }
#else
    Dl_info info;
    if (dladdr((void*)GetCurrentModuleFolderPath, &info))
    {
        return fs::path(info.dli_fname).remove_filename();
    }
#endif
    return {};
}

static ::shared::WSTRING GetDatadogLogsDirectoryPath()
{
    ::shared::WSTRING directory = shared::GetEnvironmentValue(cfg_log_directory_env);

    if (directory.length() > 0)
    {
        return directory;
    }

#ifdef _WIN32
    fs::path program_data_path = shared::GetEnvironmentValue(WStr("PROGRAMDATA"));

    if (program_data_path.empty())
    {
        program_data_path = WStr(R"(C:\ProgramData)");
    }

    // on Windows WSTRING == wstring
    return (program_data_path / datadog_logs_folder_path).wstring();
#else
    return shared::ToWSTRING("/var/log/datadog/dotnet/");
#endif
}

// Gets the configuration file path
static fs::path GetConfigurationFilePath()
{
    fs::path env_configfile = shared::GetEnvironmentValue(cfg_filepath_env);

    if (!env_configfile.empty())
    {
        // In 2.14.0, we have moved this file and the config may point to a path where it's not present
        // So we check if we can find it, if not, we default to the current module folder
        std::error_code ec; // fs::exists might throw if no error_code parameter is provided
        if (fs::exists(env_configfile, ec))
        {
            return env_configfile;
        }
        Log::Warn("File set in '", cfg_filepath_env, "' doesn't exist. Using the default path");
    }

    return GetCurrentModuleFolderPath() / conf_filename;
}

inline bool IsRunningOnIIS()
{
    const auto& process_name = ::shared::GetCurrentProcessName();
    return process_name == WStr("w3wp.exe") || process_name == WStr("iisexpress.exe");
}

inline std::string GetCurrentOsArch(bool isRunningOnAlpine)
{
#if AMD64

#if _WINDOWS
    return "win-x64";
#elif LINUX
    return isRunningOnAlpine ? "linux-musl-x64" : "linux-x64";
#elif MACOS
    return "osx-x64";
#else
#error "currentOsArch not defined."
#endif

#elif X86

#if _WINDOWS
    return "win-x86";
#elif LINUX
    return isRunningOnAlpine ? "linux-musl-x86" : "linux-x86";
#elif MACOS
    return "osx-x86";
#else
#error "currentOsArch not defined."
#endif

#elif ARM64

#if _WINDOWS
    return "win-arm64";
#elif LINUX
    return isRunningOnAlpine ? "linux-musl-arm64" : "linux-arm64";
#elif MACOS
    return "osx-arm64";
#else
#error "currentOsArch not defined."
#endif

#elif ARM

#if _WINDOWS
    return "win-arm";
#elif LINUX
    return isRunningOnAlpine ? "linux-musl-arm" : "linux-arm";
#elif MACOS
    return "osx-arm";
#else
#error "currentOsArch not defined."
#endif

#else
#error "currentOsArch not defined."
#endif
}