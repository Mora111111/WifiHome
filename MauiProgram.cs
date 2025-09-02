using Microsoft.Extensions.Logging;
using WifiHome.Services;
using WifiHome.Utilities;
using WifiHome.Views;

namespace WifiHome
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // تسجيل الخدمة حسب النظام الأساسي
#if ANDROID
            builder.Services.AddSingleton<IWifiTrackerService, Platforms.Android.Services.AndroidWifiTrackerService>();
#endif

            // إضافة خدمة UDP
            builder.Services.AddSingleton<UdpBroadcastService>();

            // إضافة المحولات
            builder.Services.AddSingleton<BoolToColorConverter>();

            // إضافة الصفحات
            builder.Services.AddTransient<AddUserPage>();
            builder.Services.AddTransient<HostModePage>();
            builder.Services.AddTransient<DebugPage>();

            return builder.Build();
        }
    }
}