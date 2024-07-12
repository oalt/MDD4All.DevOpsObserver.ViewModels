using MDD4All.DevOpsObserver.DataModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace MDD4All.DevOpsObserver.ViewModels
{
    public class StatusViewModel
    {

        private DevOpsStatusInformation _statusInformation;

        public StatusViewModel(DevOpsStatusInformation statusInformation) 
        {
            _statusInformation = statusInformation;
        }

        public string CssClass
        {
            get
            {
                string result = "card ";

                if(_statusInformation.Status == DevOpsStatus.Unknown)
                {
                    result += "statusUnknown";
                }
                else if(_statusInformation.Status == DevOpsStatus.Success) 
                {
                    result += "statusSuccess";
                }
                else if(_statusInformation.Status == DevOpsStatus.Fail)
                {
                    result += "statusFail";
                }
                else if (_statusInformation.Status == DevOpsStatus.Error)
                {
                    result += "statusError";
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

    }
}
