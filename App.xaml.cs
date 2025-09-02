namespace WifiHome
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        protected override void OnStart()
        {
            // يمكن إضافة كود عند بدء التطبيق هنا
        }

        protected override void OnSleep()
        {
            // يمكن إضافة كود عند إدخال التطبيق في الخلفية هنا
        }

        protected override void OnResume()
        {
            // يمكن إضافة كود عند استئناف التطبيق هنا
        }
    }
}