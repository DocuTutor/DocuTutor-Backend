using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DocuTutor.Domain.Entities
{
    public class ApplicationUser :IdentityUser
    {
        public string? FullName { get; private set; }

        public bool IsActive { get; private set; }

        public DateTime? LastLoginAt { get; private set; }

        public DateTime CreatedAt { get; private set; }

        public DateTime UpdatedAt { get; private set; }
        public virtual ICollection<RefreshToken>? RefreshTokens { get; set; } = new List<RefreshToken>();

    
    }
}
