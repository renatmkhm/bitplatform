﻿using System.Reflection;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI.Demo.Client.Core;

public partial class App
{
#if BlazorWebAssembly && !BlazorHybrid
    private List<Assembly> _lazyLoadedAssemblies = new();
    [AutoInject] private Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader _assemblyLoader = default!;
#endif

    [AutoInject] private IJSRuntime _jsRuntime = default!;
    [AutoInject] private IBitDeviceCoordinator _bitDeviceCoordinator { get; set; } = default!;

    private bool _cultureHasNotBeenSet = true;

#if BlazorHybrid
    protected override async Task OnInitializedAsync()
    {
        SetupBodyClasses();
        await base.OnInitializedAsync();
    }
#else
    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            SetupBodyClasses();
        }

        base.OnAfterRender(firstRender);
    }
#endif

    private void SetupBodyClasses()
    {
        var cssClasses = new List<string>();

        if (BlazorModeDetector.Current.IsBlazorWebAssembly())
        {
            cssClasses.Add("bit-blazor-wasm");
        }
        else if (BlazorModeDetector.Current.IsBlazorServer())
        {
            cssClasses.Add("bit-blazor-server");
        }
        else if (BlazorModeDetector.Current.IsBlazorHybrid())
        {
            cssClasses.Add("bit-blazor-hybrid");

            if (OperatingSystem.IsWindows())
            {
                cssClasses.Add("bit-windows");
            }
            else if (OperatingSystem.IsLinux())
            {
                cssClasses.Add("bit-linux");
            }
            else if (OperatingSystem.IsMacOS() || OperatingSystem.IsMacCatalyst())
            {
                cssClasses.Add("bit-macos");
            }
            else if (OperatingSystem.IsIOS() && OperatingSystem.IsMacCatalyst() is false)
            {
                cssClasses.Add("bit-ios");
            }
            else if (OperatingSystem.IsAndroid())
            {
                cssClasses.Add("bit-android");
            }
        }
        else if (BlazorModeDetector.Current.IsBlazorElectron())
        {
            cssClasses.Add("bit-blazor-electron");
        }

        var cssVariables = new Dictionary<string, string>();
        var statusBarHeight = _bitDeviceCoordinator.GetStatusBarHeight();

        if (OperatingSystem.IsIOS() && OperatingSystem.IsMacCatalyst() is false)
        {
            //This is handled in css using safe-area env() variables
            statusBarHeight = 0;
        }

        cssVariables.Add("--bit-status-bar-height", $"{statusBarHeight}px");
        _ = _jsRuntime.ApplyBodyElementClasses(cssClasses, cssVariables);
    }

    private async Task OnNavigateAsync(NavigationContext args)
    {
        // Blazor Server & Pre Rendering use created cultures in UseRequestLocalization middleware
        // Android, windows and iOS have to set culture programmatically.
        // Browser is gets handled in Web project's Program.cs
#if BlazorHybrid && MultilingualEnabled
        if (_cultureHasNotBeenSet)
        {
            _cultureHasNotBeenSet = false;
            var preferredCultureCookie = Preferences.Get(".AspNetCore.Culture", null);
            CultureInfoManager.SetCurrentCulture(preferredCultureCookie);
        }
#endif

#if BlazorWebAssembly && !BlazorHybrid
        if (args.Path.Contains("chart") && _lazyLoadedAssemblies.Any(asm => asm.GetName().Name == "Newtonsoft.Json") is false)
        {
            var assemblies = await _assemblyLoader.LoadAssembliesAsync(new[] { "Newtonsoft.Json.dll", "System.Private.Xml.dll", "System.Data.Common.dll" });
            _lazyLoadedAssemblies.AddRange(assemblies);
        }
#endif
    }
}