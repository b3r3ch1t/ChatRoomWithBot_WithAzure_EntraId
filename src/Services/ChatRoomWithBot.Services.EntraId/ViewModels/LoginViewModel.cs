﻿using System.ComponentModel.DataAnnotations;

namespace ChatRoomWithBot.Services.EntraId.ViewModels
{
    public  class LoginViewModel
    {
        [Required(ErrorMessage = "EMAIL_REQUIRED")]
        [EmailAddress(ErrorMessage = "EMAIL_INVALID")]
        public string Email { get; set; }
        [Required(ErrorMessage = "PASSWORD_REQUIRED")]
        public string Password { get; set; }
        public bool RememberLogin { get; set; }
        public string? ReturnUrl { get; set; }
    }
}
