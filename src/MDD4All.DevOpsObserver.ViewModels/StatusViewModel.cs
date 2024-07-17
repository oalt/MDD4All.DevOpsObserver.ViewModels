using MDD4All.DevOpsObserver.DataModels;
using MDD4All.DevOpsObserver.StatusCalculation;
using System;
using System.Threading;

namespace MDD4All.DevOpsObserver.ViewModels
{
    public class StatusViewModel : IComparable<StatusViewModel>
    {

        private DevOpsStatusInformation _statusInformation;

        private StatusCalculator _statusCalculator;

        public StatusViewModel(DevOpsStatusInformation statusInformation)
        {
            _statusInformation = statusInformation;
            _statusCalculator = new StatusCalculator();
        }

        public string CssClass
        {
            get
            {
                string result = "card ";

                DevOpsStatus status = _statusCalculator.CalculateDisplayStatus(_statusInformation);

                if (status == DevOpsStatus.Unknown)
                {
                    result += "statusUnknown";
                }
                else if (status == DevOpsStatus.Success)
                {
                    result += "statusSuccess";
                }
                else if (status == DevOpsStatus.Fail)
                {
                    result += "statusFail";
                }
                else if (status == DevOpsStatus.Warning)
                {
                    result += "statusWarning";
                }
                else if (_statusInformation.Status == DevOpsStatus.Error)
                {
                    result += "statusError";
                }

                return result;
            }
        }

        public string IconCssClass
        {
            get
            {
                string result = "";

                if(_statusInformation.GitServerType == "Github")
                {
                    result = "bi bi-github";
                }
                else if(_statusInformation.GitServerType == "Bitbucket")
                {
                    result = "bi bi-git";
                }

                return result;
            }
        }

        public DevOpsStatusInformation Status
        {
            get
            {
                return _statusInformation;
            }
        }

        public int CompareTo(StatusViewModel other)
        {
            int result = 0;

            if (Status.Status == DevOpsStatus.Unknown)
            {
                if (other.Status.Status != DevOpsStatus.Unknown)
                {
                    result = 1;
                }
            }
            else if (Status.Status == DevOpsStatus.Success)
            {
                if (other.Status.Status == DevOpsStatus.Error || other.Status.Status == DevOpsStatus.Fail)
                {
                    result = 1;
                }
            }
            else if (Status.Status == DevOpsStatus.Fail)
            {
                if (other.Status.Status == DevOpsStatus.Error)
                {
                    result = 1;
                }
                else
                {
                    result = 0;
                }
            }
            else if (Status.Status == DevOpsStatus.Error)
            {
                if (other.Status.Status == DevOpsStatus.Error)
                {
                    result = 0;
                }
                else
                {
                    result = -1;
                }
            }

            return result;
        }
    }

}

