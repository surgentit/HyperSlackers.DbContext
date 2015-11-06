﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperSlackers.AspNet.Identity.EntityFramework
{
    /// <summary>
    /// Linker that ties a role to a group
    /// </summary>
    public class HyperGroupRoleGuid : HyperGroupRole<Guid>
    {
        public HyperGroupRoleGuid()
        {
            this.Id = Guid.NewGuid();
        }
    }

    /// <summary>
    /// Linker that ties a role to a group
    /// </summary>
    public class HyperGroupRoleInt : HyperGroupRole<int>
    {

    }

    /// <summary>
    /// Linker that ties a role to a group
    /// </summary>
    public class HyperGroupRoleLong : HyperGroupRole<long>
    {

    }

    /// <summary>
    /// Linker that ties a role to a group
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    public class HyperGroupRole<TKey> : IAuditable<TKey>
        where TKey : struct, IEquatable<TKey>
    {
        [Key]
        public TKey Id { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Index("IX_ClusteredKey", IsClustered = true)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ClusteredKey { get; protected set; }

        [Required]
        [Index("IX_Group_Role", 0, IsUnique = true)]
        [Index("IX_Role_Group", 1, IsUnique = true)]
        public TKey GroupId { get; set; }

        [Required]
        [Index("IX_Group_Role", 1, IsUnique = true)]
        [Index("IX_Role_Group", 0, IsUnique = true)]
        public TKey RoleId { get; set; }
    }
}
