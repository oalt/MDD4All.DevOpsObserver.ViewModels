using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.DevOpsObserver.DataModels;
using MDD4All.DevOpsObserver.DataProviders.Integration;
using MDD4All.DevOpsObserver.StatusCalculation;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;

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

        public MainViewModel(IConfiguration configuration, HttpClient httpClient,
                             DevOpsConfiguration devOpsConfiguration)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _devOpsConfiguration = devOpsConfiguration;

            _integrationStatusProvider = new IntegrationStatusProvider(configuration, httpClient);

            _timer = new Timer();
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Interval = 300000; // 5 minutes
            _timer.Start();

            RefreshStatusDataAsync();
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

        public StatusViewModel OverallStatus
        {
            get
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

                StatusViewModel result = new StatusViewModel(statusInformation);

                return result;
            }
        }

    }
}
