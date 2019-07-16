namespace HyperSlackers.DbContext.Demo.Migrations
{
    using AspNet.Identity.EntityFramework;
    using Models;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;
    using System.Web;
    using Microsoft.AspNet.Identity.Owin;
    using System.Threading.Tasks;

    internal sealed class Configuration : DbMigrationsConfiguration<HyperSlackers.DbContext.Demo.Models.ApplicationDbContext>
    {
        public Configuration()
        {
            // DRM Changed
            //x AutomaticMigrationsEnabled = false;
            AutomaticMigrationsEnabled = true;

            // DRM Added
            // This SqlGenerator sets [KEY] columns to be NONCLUSTERED so we can define our own clustered index on the models
            SetSqlGenerator("System.Data.SqlClient", new HyperSqlServerMigrationSqlGenerator());
        }

        protected override void Seed(HyperSlackers.DbContext.Demo.Models.ApplicationDbContext context)
        {
            //  This method will be called after migrating to the latest version.

            //  You can use the DbSet<T>.AddOrUpdate() helper extension method
            //  to avoid creating duplicate seed data. E.g.
            //
            //    context.People.AddOrUpdate(
            //      p => p.FullName,
            //      new Person { FullName = "Andrew Peters" },
            //      new Person { FullName = "Brice Lambson" },
            //      new Person { FullName = "Rowan Miller" }
            //    );
            //

            // DRM Added

            Task.Run(async () => await SeedAsync(context));

        }

        private async Task SeedAsync(HyperSlackers.DbContext.Demo.Models.ApplicationDbContext context)
        {
            // grab the managers
            var hostManager = new ApplicationHostManager(new HyperHostStoreGuid<ApplicationUser>(context));
            var roleManager = new ApplicationRoleManager(new HyperRoleStoreGuid<ApplicationUser>(context));
            var userManager = new ApplicationUserManager(new HyperUserStoreGuid<ApplicationUser>(context));

            // don't audit this one!
            var auditingEnabled = context.AuditingEnabled;
            context.AuditingEnabled = false;

            // add the default host (will respond to "127.0.0.1" in URL)
            var systemHost = await hostManager.GetSystemHostAsync();
            if (systemHost == null)
            {
                systemHost = new HyperHostGuid() { Name = "<system>", IsSystemHost = true };
                systemHost.Domains.Add(new HyperHostDomainGuid() { DomainName = "127.0.0.1" });
                await hostManager.CreateAsync(systemHost);
            }

            // we can re-enable auditing here if we want, or at the end if we don't want to audit this stuff
            //context.AuditingEnabled = auditingEnabled;

            // add the "other" host (will respond to "localhost" in URL)
            var localHost = await hostManager.FindByDomainAsync("localhost");
            if (localHost == null)
            {
                localHost = new HyperHostGuid() { Name = "localhost" };
                await hostManager.AddDomainAsync(localHost, "localhost");
                await hostManager.AddDomainAsync(localHost, "abc.com");
                await hostManager.AddDomainAsync(localHost, "xyz.com");

                await hostManager.CreateAsync(localHost);

                context.SaveChanges();
            }

            // add some global roles
            AddRole(context, systemHost.Id, "Super", true, true);
            AddRole(context, systemHost.Id, "User", true);

            // roles for 127.0.0.1
            AddRole(context, systemHost.Id, "Admin");
            AddRole(context, systemHost.Id, "Author");
            AddRole(context, systemHost.Id, "Editor");
            AddRole(context, systemHost.Id, "Player");

            // roles for localhost
            AddRole(context, localHost.Id, "Admin");
            AddRole(context, localHost.Id, "Author");
            AddRole(context, localHost.Id, "Editor");
            AddRole(context, localHost.Id, "Customer");
            AddRole(context, localHost.Id, "Supervisor");
            AddRole(context, localHost.Id, "Salesperson");

            // add some groups
            AddGroup(context, localHost.Id, "Manager", false, new string[] { "Author", "Editor", "Supervisor", "Salesperson", "User" });
            AddGroup(context, localHost.Id, "AssistantManager", false, new string[] { "Author", "Editor", "Salesperson", "User" });


            context.SaveChanges();

            // create some users

            ApplicationUser user = new ApplicationUser() { HostId = systemHost.Id, UserName = "anonymous", Email = "anonymous@systemhost.com", IsGlobal = true };
            var result = await userManager.CreateAsync(user, Guid.NewGuid().ToString());
            if (result.Succeeded)
            {
                context.SaveChanges();
                await userManager.AddToRoleAsync(user.Id, "User", true); // global user role
                await userManager.AddToRoleGroupAsync(localHost.Id, user.Id, "Manager"); // localhost's manager group
                context.SaveChanges();
            }

            // super
            user = new ApplicationUser() { HostId = systemHost.Id, UserName = "super@systemhost.com", Email = "super@systemhost.com", IsGlobal = true };
            result = await userManager.CreateAsync(user, "super_system");
            if (result.Succeeded)
            {
                context.SaveChanges();
                await userManager.AddToRoleAsync(user.Id, "Super");
                context.SaveChanges();
            }
            // admin - system
            user = new ApplicationUser() { HostId = systemHost.Id, UserName = "admin@systemhost.com", Email = "admin@systemhost.com" };
            result = await userManager.CreateAsync(user, "admin_system");
            if (result.Succeeded)
            {
                context.SaveChanges();
                await userManager.AddToRoleAsync(systemHost.Id, user.Id, "Admin"); // systemhost's admin role
                context.SaveChanges();
            }
            // admin - localhost
            user = new ApplicationUser() { HostId = localHost.Id, UserName = "admin@localhost.com", Email = "admin@localhost.com" };
            result = await userManager.CreateAsync(user, "admin_local");
            if (result.Succeeded)
            {
                context.SaveChanges();
                await userManager.AddToRoleAsync(localHost.Id, user.Id, "Admin"); // localhost's admin role
                context.SaveChanges();
            }
            // bob - system (NOT GLOBAL)
            user = new ApplicationUser() { HostId = systemHost.Id, UserName = "bob@localhost.com", Email = "bob@localhost.com" };
            result = await userManager.CreateAsync(user, "bob_system");
            if (result.Succeeded)
            {
                context.SaveChanges();
                await userManager.AddToRoleAsync(user.Id, "User", true); // global user role
                await userManager.AddToRoleGroupAsync(systemHost.Id, user.Id, "Player", true); // system player role (it's not a global role, so global param will get ignored)
                context.SaveChanges();
            }
            // bob - localhost (NOT GLOBAL)
            user = new ApplicationUser() { HostId = localHost.Id, UserName = "bob@localhost.com", Email = "bob@localhost.com" };
            result = await userManager.CreateAsync(user, "bob_local");
            if (result.Succeeded)
            {
                context.SaveChanges();
                await userManager.AddToRoleAsync(user.Id, "User", true); // global user role
                await userManager.AddToRoleGroupAsync(localHost.Id, user.Id, "Manager"); // localhost's manager group
                context.SaveChanges();
            }

            // turn auditing back on
            context.AuditingEnabled = auditingEnabled;
        }


        // DRM Added
        private void AddRole(ApplicationDbContext context, Guid hostId, string roleName, bool isGlobal = false, bool isGlobalOnly = false)
        {
            var role = context.Roles.SingleOrDefault(r => r.HostId == hostId && r.Name == roleName);
            if (role == null)
            {
                context.Roles.Add(new HyperRoleGuid() { HostId = hostId, Name = roleName, IsGlobal = isGlobal, IsGlobalOnly = isGlobalOnly });

                context.SaveChanges();
            }
        }

        // DRM Added
        private void AddGroup(ApplicationDbContext context, Guid hostId, string groupName, bool isGlobal, string[] roles)
        {
            var group = context.RoleGroups.SingleOrDefault(g => g.HostId == hostId && g.Name == groupName);
            if (group == null)
            {
                context.RoleGroups.Add(new HyperRoleGroupGuid() { HostId = hostId, Name = groupName, IsGlobal = isGlobal });

                context.SaveChanges();

                group = context.RoleGroups.SingleOrDefault(g => g.HostId == hostId && g.Name == groupName);
            }

            foreach (var item in roles)
            {
                var role = context.Roles.SingleOrDefault(r => r.HostId == hostId && r.Name == item); // look for host role
                if (role == null)
                {
                    role = context.Roles.SingleOrDefault(r => r.IsGlobal == true && r.Name == item); // look for global role
                }

                var gr = context.RoleGroupRoles.SingleOrDefault(rgr => rgr.RoleGroupId == group.Id && rgr.RoleId == role.Id);
                if (gr == null)
                {
                    context.RoleGroupRoles.Add(new HyperRoleGroupRoleGuid() { RoleGroupId = group.Id, RoleId = role.Id });
                }
            }

            context.SaveChanges();
        }
    }
}
