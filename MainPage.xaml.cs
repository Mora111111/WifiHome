using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Layouts;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using WifiHome.Models;
using WifiHome.Services;
using WifiHome.Utilities;
using WifiHome.Views;

namespace WifiHome
{
    public partial class MainPage : ContentPage
    {
        // أصبح الآن قابلاً للـ null
        private WifiUsageData? _usageData;
        private readonly IWifiTrackerService _wifiTrackerService;
        private readonly UdpBroadcastService _udpService;
        private List<ConnectedDevice> _connectedDevices = new List<ConnectedDevice>();

        public MainPage(IWifiTrackerService wifiTrackerService, UdpBroadcastService udpService)
        {
            InitializeComponent();
            _wifiTrackerService = wifiTrackerService;
            _udp_service = udpService;

            SetupUdpEvents();
            LoadData();
            BindingContext = this;

            // إضافة زر التصحيح (اختياري)
            AddDebugButton();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadData();
        }

        private void LoadData()
        {
            _usageData = StorageManager.LoadUsageData();

            // مزامنة حالة التتبع مع الخدمة الفعلية
            try
            {
                bool isActuallyTracking = _wifiTrackerService.IsTracking();
                if ((_usageData?.IsTracking ?? false) != isActuallyTracking)
                {
                    // تأكد من أن لدينا كائن قبل التعيين
                    _usageData ??= new WifiUsageData();
                    _usageData.IsTracking = isActuallyTracking;
                    StorageManager.SaveUsageData(_usageData);
                }

                UpdateBindings();
                UpdateButtonText();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("خطأ في الصفحة الرئيسية", ex);
            }
        }

        private void UpdateBindings()
        {
            OnPropertyChanged(nameof(StatusMessage));
            OnPropertyChanged(nameof(DeviceName));
            OnPropertyChanged(nameof(DownloadText));
            OnPropertyChanged(nameof(UploadText));
            OnPropertyChanged(nameof(TotalText));
            OnPropertyChanged(nameof(LastUpdateText));
            OnPropertyChanged(nameof(IsStatsVisible));
        }

        private void SetupUdpEvents()
        {
            _udpService.DeviceDiscovered += OnDeviceDiscovered;
            _udpService.ErrorOccurred += OnUdpError;
        }

        private void OnDeviceDiscovered(object sender, ConnectedDevice device)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var existingDevice = _connectedDevices.FirstOrDefault(d => d.DeviceId == device.DeviceId);
                if (existingDevice != null)
                {
                    existingDevice.DownloadBytes = device.DownloadBytes;
                    existingDevice.UploadBytes = device.UploadBytes;
                    existingDevice.TotalBytes = device.TotalBytes;
                    existingDevice.LastSeen = DateTime.Now;
                    existingDevice.IsOnline = true;
                }
                else
                {
                    _connectedDevices.Add(device);
                }

                StorageManager.SaveConnectedDevices(_connectedDevices);
            });
        }

        private void OnUdpError(object sender, string errorMessage)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ErrorLogger.LogError("خطأ في الصفحة الرئيسية", new Exception(errorMessage));
            });
        }

        // الخصائص المرتبطة بالواجهة (مع فحص null)
        public string StatusMessage => (_usageData?.IsTracking ?? false) ? "جاري تسجيل الاستهلاك..." : "التسجيل متوقف";
        public string DeviceName => $"الجهاز: {_usageData?.DeviceName ?? "-"}";
        public string DownloadText => FormatBytes(_usageData?.DownloadBytes ?? 0);
        public string UploadText => FormatBytes(_usageData?.UploadBytes ?? 0);
        public string TotalText => FormatBytes(_usageData?.TotalBytes ?? 0);
        public string LastUpdateText => _usageData?.LastUpdate is DateTime dt ? $"آخر تحديث: {dt:yyyy-MM-dd HH:mm}" : "آخر تحديث: -";
        public bool IsStatsVisible => (_usageData?.TotalBytes ?? 0) > 0;

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }

        private void UpdateButtonText()
        {
            try
            {
                // تأكد من وجود _usageData قبل الوصول للخاصية
                bool tracking = _usageData?.IsTracking ?? false;
                if (StartStopButton != null)
                    StartStopButton.Text = tracking ? "إيقاف التسجيل" : "بدء التسجيل";
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("خطأ في الصفحة الرئيسية", ex);
            }
        }

        // معالجات أحداث الأزرار
        private async void OnStartStopClicked(object sender, EventArgs e)
        {
            try
            {
                // تأكد من وجود كائن _usageData قبل التعديل
                _usageData ??= StorageManager.LoadUsageData() ?? new WifiUsageData();

                if (_usageData.IsTracking)
                {
                    _wifiTrackerService.StopTracking();
                    _usageData.IsTracking = false;
                }
                else
                {
                    _wifiTrackerService.StartTracking();
                    _usageData.IsTracking = true;
                }

                StorageManager.SaveUsageData(_usageData);
                UpdateBindings();
                UpdateButtonText();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("خطأ في الصفحة الرئيسية", ex);
            }
        }

        private async void OnShowUsageClicked(object sender, EventArgs e)
        {
            try
            {
                LoadData();
                await DisplayAlert("إحصاءات الاستهلاك",
                    $"{DeviceName}\nالتحميل: {DownloadText}\nالترفيع: {UploadText}\nالمجموع: {TotalText}\n{LastUpdateText}",
                    "موافق");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("خطأ في الصفحة الرئيسية", ex);
            }
        }

        private async void OnAddUserClicked(object sender, EventArgs e)
        {
            try
            {
                var addUserPage = Handler.MauiContext.Services.GetService<AddUserPage>();
                await Navigation.PushAsync(addUserPage);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("خطأ في الصفحة الرئيسية", ex);
            }
        }

        private async void OnWipeDataClicked(object sender, EventArgs e)
        {
            try
            {
                string password = await DisplayPromptAsync("مسح البيانات",
                    "أدخل كلمة المرور للمتابعة:",
                    "موافق", "إلغاء",
                    maxLength: 20,
                    keyboard: Keyboard.Text);

                if (password == "AMR192002")
                {
                    bool confirm = await DisplayAlert("تأكيد",
                        "هل أنت متأكد من أنك تريد مسح جميع بيانات الاستهلاك؟ لا يمكن التراجع عن هذه العملية.",
                        "نعم، مسح البيانات", "إلغاء");

                    if (confirm)
                    {
                        _wifiTrackerService.StopTracking();
                        StorageManager.ResetData();
                        LoadData();
                        UpdateBindings();
                        UpdateButtonText();
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("خطأ في الصفحة الرئيسية", ex);
            }
        }

        private async void OnHostModeClicked(object sender, EventArgs e)
        {
            try
            {
                string password = await DisplayPromptAsync("وضع المضيف",
                    "أدخل كلمة المرور للدخول إلى وضع المضيف:", "موافق", "إلغاء");

                if (password == "AMR192002")
                {
                    var hostModePage = Handler.MauiContext.Services.GetService<HostModePage>();
                    await Navigation.PushAsync(hostModePage);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("خطأ في الصفحة الرئيسية", ex);
            }
        }

        private void AddDebugButton()
        {
            var debugButton = new Button
            {
                Text = "التصحيح",
                BackgroundColor = Colors.Gray,
                TextColor = Colors.White,
                CornerRadius = 20,
                WidthRequest = 60,
                HeightRequest = 60,
                Margin = new Thickness(0, 0, 20, 20),
                HorizontalOptions = LayoutOptions.End,
                VerticalOptions = LayoutOptions.End
            };

            debugButton.Clicked += async (s, e) =>
            {
                try
                {
                    var debugPage = new DebugPage();
                    await Navigation.PushAsync(debugPage);
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogError("خطأ في الصفحة الرئيسية", ex);
                }
            };

            if (Content is ScrollView scrollView)
            {
                var absoluteLayout = new AbsoluteLayout();
                absoluteLayout.Children.Add(scrollView);
                absoluteLayout.Children.Add(debugButton);
                AbsoluteLayout.SetLayoutBounds(debugButton, new Rect(1, 1, 60, 60));
                AbsoluteLayout.SetLayoutFlags(debugButton, AbsoluteLayoutFlags.PositionProportional);

                Content = absoluteLayout;
            }
        }

        // دوال الاختبار (يمكن حذفها لاحقاً)
        private void AddTestData()
        {
            var testData = new WifiUsageData
            {
                DownloadBytes = 1024 * 1024 * 5,
                UploadBytes = 1024 * 1024 * 2,
                TotalBytes = 1024 * 1024 * 7,
                LastUpdate = DateTime.Now,
                IsTracking = true
            };

            StorageManager.SaveUsageData(testData);
            LoadData();
        }

        private void AddTestDevices()
        {
            var testDevices = new List<ConnectedDevice>
            {
                new ConnectedDevice { DeviceName = "هاتف أحمد", DownloadBytes = 1024 * 1024 * 10, UploadBytes = 1024 * 1024 * 3 },
                new ConnectedDevice { DeviceName = "تابع محمد", DownloadBytes = 1024 * 1024 * 15, UploadBytes = 1024 * 1024 * 5 },
                new ConnectedDevice { DeviceName = "لابتوب سارة", DownloadBytes = 1024 * 1024 * 25, UploadBytes = 1024 * 1024 * 8 }
            };

            StorageManager.SaveConnectedDevices(testDevices);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            _udpService.StopBroadcasting();
            _udpService.StopListening();
        }
    }
}
