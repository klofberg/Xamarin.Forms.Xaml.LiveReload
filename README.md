# Live reload for Xamarin.Forms XAML pages
Live reloading of XAML pages in the iOS and Android simulators when saving changes to XAML files from Visual Studio.

## Build Status
[![klofberg MyGet Build Status](https://www.myget.org/BuildSource/Badge/klofberg?identifier=5400c12d-0f4f-4552-be02-0a0030393b4f)](https://www.myget.org/)

## Instructions
1. Install nuget package `Xamarin.Forms.Xaml.LiveReload` from
    https://www.myget.org/F/klofberg/api/v3/index.json

2. Add the following to your Xamarin.Forms App class
```
    public partial class App : Application
    {
        protected override void OnStart()
        {
            #if DEBUG
            Xamarin.Forms.Xaml.LiveReload.LiveReload.Enable(this, exception =>
            {
                System.Diagnostics.Debug.WriteLine(exception);
            });
            #endif
        }
    }
```
3. Start the live reload server from your solution directory
    .\packages\Xamarin.Forms.Xaml.LiveReload.*\livereload.exe
    
4. Debug the app from Visual Studio, change a XAML file to see it reload automatically in the simulator