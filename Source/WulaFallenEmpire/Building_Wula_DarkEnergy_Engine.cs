using RimWorld;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace WulaFallenEmpire
{
    [StaticConstructorOnStartup]
    public class Building_Wula_DarkEnergy_Engine : Building_GravEngine
    {
        // 子类自己的静态只读字段
        private static readonly string _childStaticValue = "Child Value";
        private static readonly CachedMaterial Wula_DarkEnergy_OrbMat = new CachedMaterial("Wula/Building/Wula_DarkEnergy_Engine_Orb", ShaderDatabase.Cutout);

        // 通过实例属性暴露静态值
        protected override string InstanceValue => _childStaticValue;
        private static readonly CachedMaterial OrbMat = new CachedMaterial("Wula/Building/Wula_DarkEnergy_Engine_Orb", ShaderDatabase.Cutout);
    }
    public abstract class Building_Wula_DarkEnergy_Engine_Parent : Building_GravEngine
    {
        // 受保护的抽象属性（实例级别）
        protected abstract string InstanceValue { get; }

        // 公共访问点
        public void PrintValue()
        {
            Console.WriteLine(InstanceValue);
        }
    }

    public class Child : Parent
    {
        // 子类自己的静态只读字段
        private static readonly string _childStaticValue = "Child Value";

        // 通过实例属性暴露静态值
        protected override string InstanceValue => _childStaticValue;
    }
}