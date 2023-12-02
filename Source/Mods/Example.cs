using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Multiplayer.Compat.Mods
{
    [MpCompatFor("author.exampleModPackagedId")]
    public class Example
    {
        public Example(ModContentPack mod)
        {

            // Example Register Lambda Delegegate
            {
                // Use for a Delegate/Lambda inside a [CompilerGenerated] class.
                // associated to a none compiler generated class (the none generated class is parent type class).
                // inside generated class will be a method (likely void return type).
                // The name will be something like <ProcessInput>b__2() parentMethod is inside the <> brackets.
                // ordinal will be compiler given number, so <ProcessInput>b__2() has ordinal of 2.
                // This is volatile and can change between mod updates.
                //
                // If it's got a field that isn't vanilla, need to write a sync worker for that field.
                // If you don't know how to check, don't worry errors probably will let you know.

                // [CompilerGenerated]
                // private sealed class <>c_DisplayClass3_0
                // {
                //     public ThingDef thing;
                //     public Command_SetGenomList<>4__this;
                //
                //     public c_DisplayClass3_0() { }
                //
                //     internal void <ProcessInput>b__2()
                //     {
                //         [...]
                //     }
                // }

                // Turns this into

                // [CompilerGenerated]
                // private sealed class <>c_DisplayClass3_0
                // {
                //     public ThingDef thing;
                //     public Command_SetGenomList<>4__this;
                //
                //     public c_DisplayClass3_0() { }
                //
                //     [SyncMethod]
                //     internal void <ProcessInput>b__2()
                //     {
                //         [...]
                //     }
                // }


                MpCompat.RegisterLambdaDelegate("GeneticRim.Command_SetGenomeList", "ProcessInput", 2);
            }



            // Example Register Lambda Method
            {
                // Use for a inside a [CompilerGenerated] method.
                // associated to a none compiler generated class (parent type class).
                // inside generated class will be a method (hopefully void return type).
                // The name will be something like <GetGizmos>b__5_1() parentMethod is inside the <> brackets.
                // ordinal will be *the end of* compiler given number, so <GetGizmos>b__5_1() has ordinal of 1.
                // This is volatile and can change between updates.
                // 
                // If it is associated to a non-vanilla class it's going to need a sync worker for the class.
                // If you don't know how to check, don't worry errors probably will let you know.

                // [CompilerGenerated]
                // private void <GetGizmos>b__5_1()
                // {
                //     this.progress = 1f
                // }

                MpCompat.RegisterLambdaMethod("GeneticRim.BuildingDNAStorageBanks", "GetGizmos", 1);
            }


        }
    }
    
}
