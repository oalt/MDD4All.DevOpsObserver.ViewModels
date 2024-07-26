using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MDD4All.DevOpsObserver.DataModels;
using MDD4All.DevOpsObserver.DataProviders.Integration;
using MDD4All.DevOpsObserver.StatusCalculation;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Input;

namespace MDD4All.DevOpsObserver.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private IConfiguration _configuration;
        private HttpClient _httpClient;
        private DevOpsConfiguration _devOpsConfiguration;

        private StatusCalculator _statusCalculator = new StatusCalculator();

        private IntegrationStatusProvider _integrationStatusProvider;

        private Timer _timer;

        private Timer _clockRefreshTimer;

        public MainViewModel(IConfiguration configuration, HttpClient httpClient,
                             DevOpsConfiguration devOpsConfiguration)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _devOpsConfiguration = devOpsConfiguration;

            _integrationStatusProvider = new IntegrationStatusProvider(configuration, httpClient);

            InitializeCommands();

            _timer = new Timer();
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Interval = 300000; // 5 minutes
            _timer.Start();

            _clockRefreshTimer = new Timer();
            _clockRefreshTimer.Interval = 1000;
            _clockRefreshTimer.AutoReset = true;
            _clockRefreshTimer.Elapsed += OnClockTimerElapsed;
            _clockRefreshTimer.Start();

            RefreshStatusDataAsync();
        }

        private void InitializeCommands()
        {
            RefreshStatusCommand = new RelayCommand(ExecuteRefreshStatusCommand);
        }

        private void OnClockTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (DateTime.Now.Second == 0)
            {
                OnPropertyChanged();
            }
        }

        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            await RefreshStatusDataAsync();
        }

        public async Task RefreshStatusDataAsync()
        {
            StatusAvailable = false;
            _refreshingStatus = true;

            OnPropertyChanged();

            List<DevOpsStatusInformation> resultingStatus = new List<DevOpsStatusInformation>();

            List<Task> tasks = new List<Task>();

            foreach (DevOpsSystem devOpsSystem in _devOpsConfiguration.Systems)
            {
                Task<List<DevOpsStatusInformation>> task = _integrationStatusProvider.GetDevOpsStatusListAsync(devOpsSystem);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            foreach (Task task in tasks)
            {
                Task<List<DevOpsStatusInformation>> finishedTask = task as Task<List<DevOpsStatusInformation>>;
                if (finishedTask != null)
                {
                    resultingStatus.AddRange(finishedTask.Result);
                }   
            }

            StatusViewModels.Clear();

            foreach (DevOpsStatusInformation devOpsStatusInformation in resultingStatus)
            {
                StatusViewModel statusViewModel = new StatusViewModel(devOpsStatusInformation);
                StatusViewModels.Add(statusViewModel);
            }

            StatusViewModels.Sort();

            LastDataRefresh = DateTime.Now;

            SetOverallStatus();

            _refreshingStatus = false;

            StatusAvailable = true;

            //Debug.WriteLine("OAS " + OverallStatus.Status.Status);

            OnPropertyChanged();

        }

        public List<StatusViewModel> StatusViewModels { get; set; } = new List<StatusViewModel>();

        private bool _refreshingStatus = false;

        public bool RefreshingStatus
        {
            get
            {
                return _refreshingStatus;
            }
        }

        public bool StatusAvailable { get; set; } = false;

        public DateTime? LastDataRefresh { get; set; } = null;

        private StatusViewModel _overallStatus = new StatusViewModel(new DevOpsStatusInformation());

        private void SetOverallStatus()
        {
            List<DevOpsStatusInformation> devOpsStatusInformationList = new List<DevOpsStatusInformation>();

            foreach (StatusViewModel statusViewModel in StatusViewModels)
            {
                devOpsStatusInformationList.Add(statusViewModel.Status);
            }

            DevOpsStatusInformation statusInformation = new DevOpsStatusInformation
            {
                Alias = "Overall Status",
                StatusValue = _statusCalculator.CalculateOverallState(devOpsStatusInformationList)
            };

            _overallStatus = new StatusViewModel(statusInformation);
        }

        public StatusViewModel OverallStatus
        {
            get
            {
                return _overallStatus;
            }
        }

        public int OnHour
        {
            get
            {
                int result = 7;
                string onHourString = _configuration["HueStatusLight:OnHour"];

                if (!string.IsNullOrEmpty(onHourString))
                {
                    int.TryParse(onHourString, out result);
                }

                return result;
            }
        }

        public int OffHour
        {
            get
            {
                int result = 18;
                string offHourString = _configuration["HueStatusLight:OffHour"];

                if (!string.IsNullOrEmpty(offHourString))
                {
                    int.TryParse(offHourString, out result);
                }

                return result;
            }
        }

        public bool TurnOffOnWeekend
        {
            get
            {
                bool result = true;

                string turnOffString = _configuration["HueStatusLight:TurnOffOnWeekend"];
                if (!string.IsNullOrEmpty(turnOffString))
                {
                    bool.TryParse(turnOffString, out result);
                }

                return result;
            }
        }


        public string CurrentTime
        {
            get
            {
                string result = DateTime.Now.ToString("HH:mm");

                return result;
            }
        }

        public int SuccessCount 
        { 
            get
            {
                int result = 0;

                result = StatusViewModels.Count(item => item.Status.StatusValue == DevOpsStatus.Success);

                return result;
            }
        }

        public int WarningCount
        {
            get
            {
                int result = 0;

                result = StatusViewModels.Count(item => item.Status.StatusValue == DevOpsStatus.Warning);

                return result;
            }
        }

        public int FailCount
        {
            get
            {
                int result = 0;

                result = StatusViewModels.Count(item => item.Status.StatusValue == DevOpsStatus.Fail);

                return result;
            }
        }

        public int ErrorCount
        {
            get
            {
                int result = 0;

                result = StatusViewModels.Count(item => item.Status.StatusValue == DevOpsStatus.Error);

                return result;
            }
        }
        public int UnknownCount
        {
            get
            {
                int result = 0;

                result = StatusViewModels.Count(item => item.Status.StatusValue == DevOpsStatus.Unknown);

                return result;
            }
        }

        public string OrganizationTitle
        {
            get
            {
                return _devOpsConfiguration.OrganizationTitle;
            }
        }

        public string AppReleaseVersion
        {
            get
            {
                string result = "";

                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                System.Diagnostics.FileVersionInfo fileVersionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);
                string version = fileVersionInfo.FileVersion;

                result = "v" + version;

                return result;
            }
        }

        public ICommand RefreshStatusCommand { get; private set; }

        private void ExecuteRefreshStatusCommand()
        {
            RefreshStatusDataAsync();
        }
    }
}
