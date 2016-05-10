using System;

namespace Xamarin.Forms.Xaml.LiveReload
{
    public static class LiveReload
    {
        public static void Enable(Application application, Action<Exception> onException)
        {
            onException(new NotImplementedException("This functionality is not implemented in the portable version of this assembly.  You should reference the NuGet package from your main application project in order to reference the platform-specific implementation."));
        }
    }
}