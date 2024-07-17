using CommunityToolkit.Mvvm.ComponentModel;
using MDD4All.DevOpsObserver.DataModels;
using MDD4All.DevOpsObserver.DataProviders.Integration;
using MDD4All.DevOpsObserver.StatusCalculation;
using Microsoft.Extensions.Configuration;
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

            _timer = new Timer(300000);
            _timer.Elapsed += OnTimerElapsed;

            Task.Run(() => RefreshStatusDataAsync()).Wait();

            _timer.AutoReset = true;
            //_timer.Enabled = true;
        }

        private async Task RefreshStatusDataAsync()
        {
            List<DevOpsStatusInformation> resultingStatus = new List<DevOpsStatusInformation>();

            foreach (DevOpsSystem devOpsSystem in _devOpsConfiguration.Systems)
            {
                List<DevOpsStatusInformation> status = await _integrationStatusProvider.GetDevOpsStatusListAsync(devOpsSystem);
                resultingStatus.AddRange(status);
            }

            StatusViewModels.Clear();

            foreach (DevOpsStatusInformation devOpsStatusInformation in resultingStatus)
            {
                StatusViewModel statusViewModel = new StatusViewModel(devOpsStatusInformation);
                StatusViewModels.Add(statusViewModel);
            }

            StatusViewModels.Sort();

            _refreshingStatus = false;

            OnPropertyChanged();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _refreshingStatus = true;
            Task.Run(() => RefreshStatusDataAsync()).Wait();
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

        public StatusViewModel OverallStatus
        {
            get
            {
                List<DevOpsStatusInformation> devOpsStatusInformationList = new List<DevOpsStatusInformation>();

                foreach(StatusViewModel statusViewModel in StatusViewModels)
                {
                    devOpsStatusInformationList.Add(statusViewModel.Status);
                }

                DevOpsStatusInformation statusInformation = new DevOpsStatusInformation
                {
                    Alias = "Overall Status",
                    Status = _statusCalculator.CalculateOverallState(devOpsStatusInformationList)
                };

                StatusViewModel result = new StatusViewModel(statusInformation);

                return result;
            }
        }

    }
}
