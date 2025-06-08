using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ShuttleMate.Contract.Repositories.Entities;
using ShuttleMate.Core.Utils;
using ShuttleMate.Repositories.Context;

namespace Wanvi.Repositories.SeedData
{
    public class ApplicationDbContextInitialiser
    {
        private readonly DatabaseContext _context;

        public ApplicationDbContextInitialiser(DatabaseContext context)
        {
            _context = context;
        }

        public void Initialise()
        {
            try
            {
                if (_context.Database.IsSqlServer())
                {
                    _context.Database.Migrate();
                    Seed();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                _context.Dispose();
            }
        }

        public void Seed()
        {
            int data = 0;

            data = _context.Roles.Count();
            if (data is 0)
            {
                Role[] roles = CreateRoles();
                _context.AddRange(roles);
            }

            data = _context.Users.Count();
            if (data is 0)
            {
                User[] user = CreateUsers();
                _context.AddRange(user);
            }
            _context.SaveChanges();

            AssignRoleToUser("admin", "Admin");

            _context.SaveChanges();
        }

        private static Role[] CreateRoles()
        {
            Role[] roles =
              [
                  new Role
            {
                Id = Guid.NewGuid(),
                Name = "Admin",
            },
              new Role
            {
                Id = Guid.NewGuid(),
                Name = "Visitor",
            },
            ];
            return roles;
        }

        private static User[] CreateUsers()
        {
            var passwordHasher = new FixedSaltPasswordHasher<User>(Options.Create(new PasswordHasherOptions()));
            User[] users =
            [
                new User
            {
                UserName = "admin",
                FullName = "Admin",
                Gender = true,
                DateOfBirth = DateTime.Now,
                ProfileImageUrl = "https://www.gravatar.com/avatar/00000000000000000000000000000000?d=mp",
                Address = "79B/1 Nguyễn Thị Tràng, Hiệp Thành, Quận 12, Hồ Chí Minh",
                PhoneNumber = "0123456789",
                Email = "admin@gmail.com",
                EmailVerified = true,
                PasswordHash = passwordHasher.HashPassword(null, "1234")
            }
            ];
            return users;
        }

        private void AssignRoleToUser(string username, string roleName)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserName == username);
            var role = _context.Roles.FirstOrDefault(r => r.Name == roleName);

            if (user != null && role != null)
            {
                if (!_context.UserRoles.Any(ur => ur.UserId == user.Id && ur.RoleId == role.Id))
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = role.Id
                    });
                    _context.SaveChanges();
                }
            }
        }
    }
}