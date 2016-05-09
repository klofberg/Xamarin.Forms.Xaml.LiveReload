# Live reload for Xamarin.Forms XAML pages
Live reloading of XAML pages in the iOS and Android simulators when saving changes to XAML files from Visual Studio.

## Build Status
![alt text](https://ci.appveyor.com/api/projects/status/q3id0ulvhb8dpoe6?svg=true "Build status")

## Instructions

1. Add the following to your Xamarin.Forms App class
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
2. Start the live reload server from your solution directory
    .\packages\Xamarin.Forms.Xaml.LiveReload.*\livereload.exe
    
3. Debug the app from Visual Studio, change a XAML file to see it reload automatically in the simulator