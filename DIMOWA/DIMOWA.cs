﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dnlib.DotNet;
using IMOWAAnotations;
using System.Reflection;
using dnlib.DotNet.Emit;
using dnpatch;
using System.IO;

namespace IMOWA
{
    class DIMOWA // Desinstalador e Instalador de Mods do Outer Wilds Alpha
    {

        //RETURN:
        // 0 - Deu tudo OK
        // 1 - Deu erro
        // 2 - Não Deu certo, mas não por erro

        static Target ModInnitTarget(Type modClass, string modName, Patcher p, string modMethodToTarget, string modClassToTarget, string modNamespaceToTarget = "", int indiceOfIntructions = 0)
        {
           

            //Padrão genérico das Instructions para os MOWA
            Instruction[] opcodesModInnit = {

                Instruction.Create(OpCodes.Ldstr   ,  $"{modName} foi iniciado | was started"),

                Instruction.Create(OpCodes.Call, p.BuildCall(typeof(UnityEngine.Debug), "Log" , typeof(void) , new[]{ typeof(object) })),

                Instruction.Create(OpCodes.Ldstr   ,  modName),

                Instruction.Create(OpCodes.Call, p.BuildCall(modClass, "ModInnit", typeof(void), new[] { typeof(string) }))
                

                };

            int[] indicesDoMod = new int[opcodesModInnit.Length];

            for (int i =0;i < indicesDoMod.Length;i++)
            {
                indicesDoMod[i] = i + indiceOfIntructions;
            }
                


            //Criar um target genérico que pode ser setado usando variaveis padrões da Classe MOWA
            Target targetMod = new Target()
            {
                Namespace = modNamespaceToTarget,
                Class = modClassToTarget, 
                Method = modMethodToTarget, 
                Instructions = opcodesModInnit,
                Indices = indicesDoMod,
                InsertInstructions = true

            };

            return targetMod;
        }

        static Target ModInnitTarget(MOWAP modMOWAP, Patcher modPatcher)
        {
            return ModInnitTarget(modMOWAP.ModType, modMOWAP.ModName, modPatcher, modMOWAP.ModMethodToTarget, modMOWAP.ModClassToTarget, modMOWAP.ModNamespaceToTarget, modMOWAP.IndiceOfIntructions);
        }

        //PORQUE ISSO NÃO PODE SER UM POINTER AHAHAHAH
        static int InstallMod(Target modInnitTarget, Patcher modPatcher)
        {
            
            try
            {
                modPatcher.Patch(modInnitTarget);

                Console.WriteLine("Os Patchings foram um sucesso, salvando agora. . . | The Patchings were a success, saving now. . .");
            }
            catch (Exception exp)
            {
                Console.WriteLine($"Erro no Patching | Patching error: {exp}");
            }


            try
            {
                //modPatcher.Save(true);
                Console.WriteLine("Mod Salvado com Sucesso :: ) | Mod saving was successfull :: )");
                return 0;
            }
            catch (Exception exp)
            {
                Console.WriteLine($"Erro no Saving | Saving error: {exp}");
                Console.WriteLine("O mod não foi possivel de ser instalado | It wasn't possible to install the mod");
                return 1;
            }

        }

        

        static int CheckIfModInstalled(Target modInnitTarget, Patcher modPatcher, int occurence = 0)
        {
            Instruction[] instructionOfMethod = modPatcher.GetInstructions(modInnitTarget);

            int comparingIndex = 0; // Do method para conferir
            int instructionIndex = 0; // Do mod

            int occurenceCounter = 0;

           

            while (instructionIndex < modInnitTarget.Instructions.Length && comparingIndex < instructionOfMethod.Length)
            {
                if (modInnitTarget.Instructions[instructionIndex].Operand == null && instructionOfMethod[comparingIndex].Operand == null)
                {
                    
                    if (modInnitTarget.Instructions[instructionIndex].OpCode.Name == instructionOfMethod[comparingIndex].OpCode.Name)
                    {
						comparingIndex++;
						instructionIndex++;
                       

                        if (instructionIndex == (modInnitTarget.Instructions.Length - 1))
                        {
                           

                            if (occurenceCounter == occurence)
                                return comparingIndex - instructionIndex;
                            else
                                occurenceCounter++;
                        }

                    }
                }
                else if (modInnitTarget.Instructions[instructionIndex].OpCode.Name == instructionOfMethod[comparingIndex].OpCode.Name && modInnitTarget.Instructions[instructionIndex].Operand.ToString() == instructionOfMethod[comparingIndex].Operand.ToString())
                {
                    
                    comparingIndex++;
					instructionIndex++;

                    if(instructionIndex == (modInnitTarget.Instructions.Length - 1))
                    {
                        

                        if (occurenceCounter == occurence)
                            return comparingIndex - instructionIndex;
                        else
                            occurenceCounter++;
                    }


                }
				else
                {
                    
                    comparingIndex = comparingIndex - instructionIndex + 1;
                    instructionIndex = 0;
                }
            }

            
            return -1;
        }

        static int UninstallMod(Target modInnitTarget, Patcher modPatcher, int modAlreadyInstalledIndex)
        {


            for (int i = 0; i < modInnitTarget.Indices.Length; i++)
            {
                modInnitTarget.Indices[i] += modAlreadyInstalledIndex;
            }

            modPatcher.RemoveInstruction(modInnitTarget);


            return 0;

        }

        static bool IsTheDllAMod(string dllName)
        {
            //Se alguem colocar na pasta /mods algum desses dlls """sem querer""" não identifica-los só por segurança
            return !(dllName == "0Harmony.dll" || dllName == "IMOWAAnotations.dll" || dllName == "dnlib.dll" || dllName == "dnpatch.dll" || 
                dllName == "HarmonyDnet2Fixes.dll" || dllName == "Mono.Security.dll" || dllName == "Assembly-CSharp.dll" || 
                dllName == "Assembly-CSharp-firstpass.dll" || dllName == "Assembly-UnityScript.dll" || dllName == "Assembly-UnityScript-firstpass.dll" ||
                dllName == "Boo.Lang.dll" || dllName == "DecalSystem.Runtime.dll" || dllName == "mscorlib.dll" || dllName == "System.Core.dll" ||
                dllName == "System.dll" || dllName == "System.Xml.dll" || dllName == "UnityEngine.dll" || dllName == "UnityEngine.dll"||
                dllName == "UnityEngine.Lang.dll");
        }

        static int InstallOrUninstallMod(Target modInnitTarget, Patcher modPatcher, int modIndex, string modDllPath, string assemblyPath)
        {
            int modStatus = -1;

            if (modIndex > -1)
            {
                Console.WriteLine('\n' + "Desinstalando| Uninstalling . . .");

                modStatus = UninstallMod(modInnitTarget, modPatcher, modIndex);

                
                if (File.Exists(assemblyPath + '/'+ modDllPath))
                {
                    Console.WriteLine("O arquivo do mod ainda existe na pasta do jogo, deletando-o . . .");
                    File.Delete(assemblyPath + '/' + modDllPath);
                }

                if (modStatus == 0)
                {
                    Console.WriteLine("O mod foi desinstalado com sucesso");
                    Console.WriteLine("The mod was sucesffuly unistalled");
                }
                else
                {
                    Console.WriteLine("O mod não foi desinstalado");
                    Console.WriteLine("The mod was not uninstalled");
                }
            }
            else
            {
                Console.WriteLine($"Instalando o mod | Instaling the mod. . .");

                
                modStatus = InstallMod(modInnitTarget, modPatcher);

                
                if (!File.Exists(assemblyPath + '/' + modDllPath))
                {
                    Console.WriteLine("O arquivo do mod não existe na pasta do jogo, copiando para la . . .");
                    File.Copy(assemblyPath + "/mods/" + modDllPath, assemblyPath + '/' + modDllPath);
                }

                if (modStatus == 0)
                {
                    Console.WriteLine("O mod foi instalado com sucesso");
                    Console.WriteLine("The mod was sucesffuly installed");
                }
                else
                {
                    Console.WriteLine("O mod não foi instalado");
                    Console.WriteLine("The mod was not istalled");
                }
            }

            return modStatus;
        }
        

        static void Main(string[] args)
        {

            List<MOWAP> listOfMods = new List<MOWAP>();

            string caminhoDessePrograma = Directory.GetCurrentDirectory();

            string[] todosOsDlls = Directory.GetFiles(caminhoDessePrograma +@"\mods\", "*.dll");



            List<string> dllsDosMods = new List<string>();

            
            for (int i =0; i < todosOsDlls.Length; i++)
            {

                if (IsTheDllAMod(todosOsDlls[i].Remove(0, caminhoDessePrograma.Count() + 6)))
                {
                    //Ver quais dlls foram aceitos como possiveis mods
                    //Console.WriteLine("Dll:");
                    //Console.WriteLine(todosOsDlls[i]);
                    dllsDosMods.Add(todosOsDlls[i]);
                }
            }
            

            //Se isso aki n é grande, eu não sei o que é \/

            //Ir em cada dll
            foreach (string filePath in dllsDosMods)
            {
                //Carregar as classes em cada dll
                Type[] classesNoDll = Assembly.LoadFrom(filePath).GetTypes();
                //Ir em cada classe 
                foreach (Type classeDoDll in classesNoDll)
                {
                    //Ir em cada método de cada classe
                    foreach (MethodInfo mInfo in classeDoDll.GetMethods())
                    {
                        //Ir em cada atributo de cada método
                        foreach (Attribute attr in
                            Attribute.GetCustomAttributes(mInfo))
                        {
                            // Ver se é o atributo que queremos
                            if (attr.GetType() == typeof(IMOWAModInnit))
                            {
                                //Montar MOWAP e adicionar a lista de mods
                                listOfMods.Add(new MOWAP()
                                {
                                    ModType = classeDoDll,

                                    ModName = ((IMOWAModInnit)attr).modName,

                                    ModMethodToTarget = ((IMOWAModInnit)attr).methodToPatch,

                                    ModClassToTarget = ((IMOWAModInnit)attr).classToPatch,

                                    ModNamespaceToTarget = ((IMOWAModInnit)attr).namespaceToPatch,

                                    IndiceOfIntructions = ((IMOWAModInnit)attr).indiceOfPatch,

                                    dllFileName = filePath.Remove(0, caminhoDessePrograma.Count() + 6),

                                });

                                //Ver os atributos dos modInnit de cada mod
                                //Console.WriteLine(((IMOWAModInnit)attr).modName + " " + ((IMOWAModInnit)attr).methodToPatch + " "+ ((IMOWAModInnit)attr).classToPatch + " "+((IMOWAModInnit)attr).namespaceToPatch );

                            }
                        }
                    }
                }
            }
            
            
           
            
            Patcher patcher = new Patcher("Assembly-CSharp.dll");


            int amountOfMods = listOfMods.Count;

            Target[] listOfModTarget = new Target[amountOfMods];

            for(int i =0;i< amountOfMods; i++)
            {
                listOfModTarget[i] = ModInnitTarget(listOfMods[i], patcher);
            }

            
            int[] indexOfModInnits= new int[amountOfMods];
            

            bool shouldTheProgamBeOpen = true;
            string resposta = "listademods";
            int indexofmod = -1;

            while (shouldTheProgamBeOpen) // Rai pf não me mata porcausa disso ;-;, isso aki é realmente uma abominação \/
            {

                //Carrega o menu com a lista de mods
                if ((resposta == "listademods" || resposta == "recarregar" || resposta == "refresh" || resposta == "menu" || resposta == "r") && indexofmod < 0)
                {
                    Console.Clear();
                    Console.WriteLine(" --- DIMOWA v.3 --- ");
                    for (int i = 0; i < listOfMods.Count; i++)
                    {
                        indexOfModInnits[i] = CheckIfModInstalled(listOfModTarget[i], patcher);
                    }

                    Console.WriteLine($" {amountOfMods} Mods Detectados/ Detected Mods: " + '\n');
                    for (int i = 0; i < amountOfMods; i++)
                    {
                        Console.WriteLine('\t' + $"{i}  * {listOfMods[i].ModName} - Status: " + ((indexOfModInnits[i] > -1) ? "Instalado / Installed" : "Não Instalado / Not Installed"));

                    }
                    Console.WriteLine('\n' + "Escreva / Write  [instalar todos / it | install all / ia] para instalar todos os mods / to install all the mods");
                    Console.WriteLine("Ou / Or escreva / write  [desinstalar todos/dt | uninstall all / ua] para desinstalar todos os mods / to uninstall all the mods");
                    Console.WriteLine("Ou tambem escolha um mod usando o numero da sequencia para mais opcoes");
                    Console.WriteLine("Or you can too choose a mod using the number of the sequence for more options");
                    Console.WriteLine("E se voce quer sair do programa digite [sair / s]");
                    Console.WriteLine("And if you want to close the program, write [close / c]");


                }
                //instalar todos os mods
                else if ((resposta == "instalar todos" || resposta == "it" || resposta == "install all" || resposta == "ia") && indexofmod < 0)
                {
                    Console.Clear();

                    for (int i = 0; i < listOfModTarget.Length; i++)
                    {
                        if (indexOfModInnits[i] < 0)
                        {
                            Console.WriteLine('\n' + $"Instalando / Installing {listOfMods[i].ModName} . . .");
                            InstallOrUninstallMod(listOfModTarget[i], patcher, indexOfModInnits[i],listOfMods[i].dllFileName, caminhoDessePrograma);
                        }
                    }

                    patcher.Save(false);
                    patcher = new Patcher("Assembly-CSharp.dll");

                    Console.WriteLine('\n' + "Todos os mods estao agora instalados");

                    Console.WriteLine('\n' + "Digite [recarregar / r / menu] para recarregar o menu");
                    Console.WriteLine("Write [refresh / r / menu] to reload the menu");
                    Console.WriteLine("E se voce quer sair do programa digite [sair / s]");
                    Console.WriteLine("And if you want to close the program, write [close / c]");
                    indexofmod = -1;
                }

                //desinstalar todos os mods
                else if ((resposta == "desinstalar todos" || resposta == "dt" || resposta == "uninstall all" || resposta == "ua") && indexofmod < 0)
                {
                    Console.Clear();

                    for (int i = 0; i < listOfModTarget.Length; i++)
                    {
                        if (indexOfModInnits[i] > -1)
                        {
                            Console.WriteLine('\n' + $"Desinstalando / Unistalling {listOfMods[i].ModName} . . .");
                            InstallOrUninstallMod(listOfModTarget[i], patcher, indexOfModInnits[i], listOfMods[i].dllFileName,caminhoDessePrograma);
                        }
                    }
                    patcher.Save(false);
                    patcher = new Patcher("Assembly-CSharp.dll");

                    Console.WriteLine('\n' + "Todos os mods estao agora desinstalados");

                    Console.WriteLine('\n' + "Digite [recarregar / r / menu] para recarregar o menu");
                    Console.WriteLine("Write [refresh / r / menu] to reload the menu");
                    Console.WriteLine("E se voce quer sair do programa digite [sair / s]");
                    Console.WriteLine("And if you want to close the program, write [close / c]");
                    indexofmod = -1;
                }

                //Se a resposta da pessoa for uma dessas e um mod tiver sido escolhido, entao instalar ou desinstalar
                else if ((resposta == "sim" || resposta == "s" || resposta == "yes" || resposta == "y") && indexofmod > -1)
                {

                    InstallOrUninstallMod(listOfModTarget[indexofmod], patcher, indexOfModInnits[indexofmod], listOfMods[indexofmod].dllFileName, caminhoDessePrograma);
                    patcher.Save(false);
                    patcher = new Patcher("Assembly-CSharp.dll");

                    Console.WriteLine('\n' + "Digite [recarregar / r / menu] para recarregar o menu");
                    Console.WriteLine("Write [refresh / r / menu] to reload the menu");
                    Console.WriteLine("E se voce quer sair do programa digite [sair / s]");
                    Console.WriteLine("And if you want to close the program, write [close / c]");
                    indexofmod = -1;
                }
                //Se for n fazer nada
                else if ((resposta == "nao" || resposta == "n" || resposta == "no") && indexofmod > -1)
                {


                    Console.WriteLine('\n' + "Digite [recarregar / r / menu] para recarregar o menu");
                    Console.WriteLine("Write [refresh / r / menu] to reload the menu");
                    Console.WriteLine("E se voce quer sair do programa digite [sair / s]");
                    Console.WriteLine("And if you want to close the program, write [close / c]");
                    indexofmod = -1;
                }
                else if ((resposta == "s" || resposta == "sair" || resposta == "c" || resposta == "close") && indexofmod < 0)
                {

                    shouldTheProgamBeOpen = false;
                }

                else
                {
                    try
                    {
                        if (indexofmod < 0)
                        {
                            indexofmod = Convert.ToInt32(resposta);

                            if (indexofmod < amountOfMods && indexofmod > -1)
                            {
                                Console.Clear();

                                bool isModinstalled = indexOfModInnits[indexofmod] > -1;

                                Console.WriteLine('\n' + $"Mod {listOfMods[indexofmod].ModName} is " + (isModinstalled ? "" : "not ") + "installed. Would you like to " + (isModinstalled ? "unnistall " : "install ") + "it?");
                                Console.WriteLine($"O mod {listOfMods[indexofmod].ModName} " + (isModinstalled ? "" : "não ") + "está instalado.Voce gostaria de " + (isModinstalled ? "desinstalar " : "instalar ") + "ele?");
                                Console.WriteLine('\n' + "For Yes -> yes / y | Para No -> no / n");
                                Console.WriteLine("Para Sim -> sim / s | Para Nao -> nao / n");
                            }
                            else
                            {
                                Console.WriteLine("Esse numero nao e um index da lista");
                                Console.WriteLine("That number is not a index of the list");
                                indexofmod = -1;
                            }

                        }
                    }
                    catch (OverflowException)
                    {
                        Console.WriteLine("O valor do numero e acima de qualquer int (32)");
                        Console.WriteLine("The value of the number is superior to any int (32)");
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine("O escrito nao e um comando valido");
                        Console.WriteLine("That's not a valid command");
                    }
                }
            
                if(shouldTheProgamBeOpen)
                    resposta = Console.ReadLine();


            }
            
        }
    }
}