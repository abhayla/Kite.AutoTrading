using System.ComponentModel.DataAnnotations;

namespace Kite.AutoTrading.Common.ViewModels
{
    public class LoginViewmodel
    {
        [Required]
        public string ZerodhaUserId { get; set; }
        [Required]
        public string ApiKey { get; set; }
        [Required]
        public string ApiSecret { get; set; }
        [Required]
        public string RequestToken { get; set; }

    }
}
