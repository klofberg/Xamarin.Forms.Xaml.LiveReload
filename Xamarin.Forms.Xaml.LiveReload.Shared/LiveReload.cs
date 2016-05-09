using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xamarin.Forms;

namespace Xamarin.Forms.Xaml.LiveReload
{
    public static class LiveReload
    {
        static Application _application;
        static readonly Regex Regex = new Regex("x:Class=\"([^\"]+)\"");
        
        public static void Enable(Application application, Action<Exception> onException)
        {
            _application = application;

            Task.Run(async () =>
            {
                var sender = new IPEndPoint(0, 0);
                using (var udp = new UdpClient(52222))
                {
                    udp.Receive(ref sender);
                }
                
                var c = new TcpClient();
                c.Connect(sender.Address.ToString(), 52222);

                c.SendMessage(new Message {MessageType = MessageType.GetHostname});
                while (c.Connected)
                {
                    try
                    {
                        var message = c.ReceiveMessage();
                        switch (message.MessageType)
                        {
                            case MessageType.XamlUpdated:
                                try
                                {
                                    var xaml = Encoding.UTF8.GetString(message.Payload, 0, message.Payload.Length);
                                    var match = Regex.Match(xaml);
                                    if (!match.Success) return;
                                    var className = match.Groups[1].Value;
                                    var page = FindPage(_application.MainPage, className);
                                    if (page == null) return;

                                    try
                                    {
                                        await UpdatePageFromXamlAsync(page, xaml);
                                    }
                                    catch (Exception exception)
                                    {
                                        await UpdatePageFromExceptionAsync(page, exception);
                                    }
                                }
                                catch (Exception exception)
                                {
                                    onException(exception);
                                }
                                break;
                        }
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode != SocketError.ConnectionReset) throw;
                        c.Close();
                    }
                }
            })
            .ContinueWith(t =>
            {
                if (t.IsFaulted) onException(t.Exception);
            });
        }
        
        static Task UpdatePageFromXamlAsync(Page page, string xaml)
        {
            var tcs = new TaskCompletionSource<object>();
            Device.BeginInvokeOnMainThread(() =>
            {
                var oldBindingContext = page.BindingContext;
                try
                {
                    LoadXaml(page, xaml);
                    page.ForceLayout();
                    tcs.SetResult(null);
                }
                catch (Exception exception)
                {
                    tcs.SetException(exception);
                }
                finally
                {
                    page.BindingContext = oldBindingContext;
                }
            });
            return tcs.Task;
        }

        static async Task UpdatePageFromExceptionAsync(Page page, Exception exception)
        {
            try
            {
                XNamespace xmlns = "http://xamarin.com/schemas/2014/forms";
                var errorPage = new XDocument(
                    new XElement(xmlns + "ContentPage",
                        new XElement("ScrollView",
                            new XAttribute("BackgroundColor", "Red"),
                            new XElement("Label",
                                new XAttribute("Text", exception.Message),
                                new XAttribute("TextColor", "White"),
                                new XAttribute("FontSize", "Small")
                                )
                            )
                        )
                    ).ToString();
                await UpdatePageFromXamlAsync(page, errorPage);
            }
            catch (Exception e)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    page.DisplayAlert("Error", exception.ToString(), "OK");
                });
            }
        }

        static Page FindPage(Page page, string fullTypeName)
        {
            if (page == null) return null;

            var p = page.Navigation.ModalStack.LastOrDefault(x => x.GetType().FullName == fullTypeName);
            if (p != null) return p;

            var navigationPage = page as NavigationPage;
            if (navigationPage?.CurrentPage.GetType().FullName == fullTypeName)
            {
                return navigationPage?.CurrentPage;
            }
            var masterDetailPage = page as MasterDetailPage;
            if (masterDetailPage != null)
            {
                p = FindPage(masterDetailPage.Master, fullTypeName);
                if (p != null) return p;

                p = FindPage(masterDetailPage.Detail, fullTypeName);
                if (p != null) return p;
            }

            if (page.GetType().FullName == fullTypeName) return page;

            return null;
        }

        static void LoadXaml(BindableObject view, string xaml)
        {
            var xamlAssembly = Assembly.Load(new AssemblyName("Xamarin.Forms.Xaml"));
            var xamlLoaderType = xamlAssembly.GetType("Xamarin.Forms.Xaml.XamlLoader");
            var loadMethod = xamlLoaderType.GetRuntimeMethod("Load", new[] { typeof(BindableObject), typeof(string) });
            try
            {
                loadMethod.Invoke(null, new object[] { view, xaml });
            }
            catch (TargetInvocationException exception)
            {
                throw exception.InnerException;
            }
        }
    }
}