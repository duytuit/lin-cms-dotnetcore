﻿using System;
using FreeSql.DataAnnotations;

namespace LinCms.Core.Entities
{
    [Table(Name = "lin_permission")]
    public class LinPermission : FullAduitEntity<long>
    {
        public LinPermission(string name, string module)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Module = module ?? throw new ArgumentNullException(nameof(module));
        }

        public LinPermission()
        {
        }

        /// <summary>
        /// 所属权限、权限名称，例如：访问首页
        /// </summary>
        [Column(DbType = "varchar(60)")]
        public string Name { get; set; }

        /// <summary>
        /// 权限所属模块，例如：人员管理
        /// </summary>
        [Column(DbType = "varchar(50)")]
        public string Module { get;  set; }

        public override string ToString()
        {
            return base.ToString() + $" Auth:{Name}、Module:{Module}";
        }
    }

}
