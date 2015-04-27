using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CBF
{

    internal delegate object DynamicMethodDelegate(object instance, object[] args);
    static class DynamicMethodDelegateFactory
    {

        public static DynamicMethodDelegate CreateDelegate(MethodInfo mi)
        {
            ParameterInfo[] args = mi.GetParameters();
            int numparams = args.Length;

            DynamicMethod dynam = new DynamicMethod("", typeof(object), new[] { typeof(object), typeof(object[]) }, mi.DeclaringType.Module,true);

            ILGenerator il = dynam.GetILGenerator();

            if (!mi.IsStatic) il.Emit(OpCodes.Ldarg_0);

            for (int i = 0; i < numparams; i++)
            {
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem_Ref);

                Type t = args[i].ParameterType;
                if (t.IsValueType) il.Emit(OpCodes.Unbox_Any, t);
            }

            if (mi.IsFinal)
                il.Emit(OpCodes.Call, mi);
            else
                il.Emit(OpCodes.Callvirt, mi);

            if (mi.ReturnType != typeof(void))
            {
                if (mi.ReturnType.IsValueType)
                    il.Emit(OpCodes.Box, mi.ReturnType);
            }
            else
                il.Emit(OpCodes.Ldnull);

            il.Emit(OpCodes.Ret);

            return (DynamicMethodDelegate)dynam.CreateDelegate(typeof(DynamicMethodDelegate));
        }
    }
}
