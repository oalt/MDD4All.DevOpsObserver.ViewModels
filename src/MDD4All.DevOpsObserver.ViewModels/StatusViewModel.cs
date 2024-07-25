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
                string result = "";

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
                else if (_statusInformation.StatusValue == DevOpsStatus.Error)
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

            if (Status.StatusValue == DevOpsStatus.Unknown)
            {
                if (other.Status.StatusValue != DevOpsStatus.Unknown)
                {
                    result = 1;
                }
            }
            else if (Status.StatusValue == DevOpsStatus.Success)
            {
                if (other.Status.StatusValue == DevOpsStatus.Error || other.Status.StatusValue == DevOpsStatus.Fail)
                {
                    result = 1;
                }
                else
                {
                    result = -1;
                }
            }
            else if (Status.StatusValue == DevOpsStatus.Fail)
            {
                if (other.Status.StatusValue == DevOpsStatus.Error)
                {
                    result = 1;
                }
                else
                {
                    result = -1;
                }
            }
            else if (Status.StatusValue == DevOpsStatus.Error)
            {
                if (other.Status.StatusValue == DevOpsStatus.Error)
                {
                    result = 1;
                }
                else
                {
                    result = -1;
                }
            }

            return result;
        }

        public override string ToString()
        {
            string result = base.ToString();

            if(Status.Alias != null)
            {
                result = Status.Alias.ToString();
                result += " " + Status.StatusValue;
            }

            

            return result;
        }
    }

}

