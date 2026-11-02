using System;
using System.Linq;
using RoR2;
using UnityEngine;
namespace SvenMod.Modules { internal static class Prefabs {
        public static void ClearEntityStateMachines(GameObject bodyPrefab)
        {
            EntityStateMachine[] machines = bodyPrefab.GetComponents<EntityStateMachine>();

            for (int i = machines.Length - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(machines[i]);
            }

            NetworkStateMachine networkMachine = bodyPrefab.GetComponent<NetworkStateMachine>();
            networkMachine.stateMachines = Array.Empty<EntityStateMachine>();

            CharacterDeathBehavior deathBehavior = bodyPrefab.GetComponent<CharacterDeathBehavior>();
            if (deathBehavior)
            {
                deathBehavior.idleStateMachine = Array.Empty<EntityStateMachine>();
            }

            SetStateOnHurt setStateOnHurt = bodyPrefab.GetComponent<SetStateOnHurt>();
            if (setStateOnHurt)
            {
                setStateOnHurt.idleStateMachine = Array.Empty<EntityStateMachine>();
            }

            CharacterBody body = bodyPrefab.GetComponent<CharacterBody>();
            if (body)
            {
                body.vehicleIdleStateMachine = Array.Empty<EntityStateMachine>();
            }
        }

        //this but in reverse https://media.discordapp.net/attachments/875473107891150878/896193331720237106/caption-7.gif?ex=65989f94&is=65862a94&hm=e1f51da3ad190c00c5da1f90269d5ef10bedb0ae063c0f20aa0dd8721608018a&
        /// <summary>
        /// Creates an EntityStateMachine, and adds it to the NetworkStateMachine, CharacterDeathBehavior, and SetStateOnHurt components. 
        /// <para>See AddMainEntityStateMachine for typically your "Body" state machine.</para>
        /// </summary>
        public static EntityStateMachine AddEntityStateMachine(GameObject prefab, string machineName, Type mainStateType = null, Type initalStateType = null, bool addToHurt = true, bool addToDeath = true)
        {
            EntityStateMachine entityStateMachine = EntityStateMachine.FindByCustomName(prefab, machineName);
            if (entityStateMachine == null)
            {
                entityStateMachine = prefab.AddComponent<EntityStateMachine>();
            }
            else
            {
                Log.Message($"An Entity State Machine already exists with the name {machineName}. replacing.");
            }
            //Set up entitystatemachine
            entityStateMachine.customName = machineName;

            if (mainStateType == null)
            {
                mainStateType = typeof(EntityStates.Idle);
            }
            entityStateMachine.mainStateType = new EntityStates.SerializableEntityStateType(mainStateType);

            if (initalStateType == null)
            {
                initalStateType = typeof(EntityStates.Idle);
            }
            entityStateMachine.initialStateType = new EntityStates.SerializableEntityStateType(initalStateType);

            //Add to NetworkStateMachine so it is networked, as it sounds
            NetworkStateMachine networkMachine = prefab.GetComponent<NetworkStateMachine>();
            if (networkMachine)
            {
                networkMachine.stateMachines = networkMachine.stateMachines.Append(entityStateMachine).ToArray();
            }

            //Add to the array of "idle" StateMachines. For when the character dies.
            //This component sets that state machine to idle, stopping what it was doing
            CharacterDeathBehavior deathBehavior = prefab.GetComponent<CharacterDeathBehavior>();
            if (deathBehavior && addToDeath)
            {
                deathBehavior.idleStateMachine = deathBehavior.idleStateMachine.Append(entityStateMachine).ToArray();
            }

            //Add to the array of "idle" StateMachines.
            //Same as CharacterDeathBehavior but for stunning/freezing/etc
            SetStateOnHurt setStateOnHurt = prefab.GetComponent<SetStateOnHurt>();
            if (setStateOnHurt && addToHurt)
            {
                setStateOnHurt.idleStateMachine = setStateOnHurt.idleStateMachine.Append(entityStateMachine).ToArray();
            }

            //Add to the array of "idle" StateMachines.
            //Same as CharacterDeathBehavior but for entering a vehicle.
            //note only the non-body states are added to this. there is no equivelent for the "main" statemachine in the AddMainEntityStateMachine function below
            CharacterBody body = prefab.GetComponent<CharacterBody>();
            if (body)
            {
                body.vehicleIdleStateMachine = body.vehicleIdleStateMachine.Append(entityStateMachine).ToArray();
            }

            return entityStateMachine;
        }

        /// <summary>
        /// Creates an EntityStateMachine, and adds it to the NetworkStateMachine, CharacterDeathBehavior, and SetStateOnHurt components.
        /// <para>Similar to AddEntityStateMachine, however when adding to these components, what we'll consider the "main state machine" (typically the "Body" state machine) has to be set in certain fields.</para>
        /// </summary>
        public static EntityStateMachine AddMainEntityStateMachine(GameObject bodyPrefab, string machineName = "Body", Type mainStateType = null, Type initalStateType = null)
        {
            EntityStateMachine entityStateMachine = EntityStateMachine.FindByCustomName(bodyPrefab, machineName);
            if (entityStateMachine == null)
            {
                entityStateMachine = bodyPrefab.AddComponent<EntityStateMachine>();
            }
            else
            {
                Log.Message($"An Entity State Machine already exists with the name {machineName}. replacing.");
            }

            //Create entitystatemachine
            entityStateMachine.customName = machineName;

            if (mainStateType == null)
            {
                mainStateType = typeof(EntityStates.GenericCharacterMain);
            }
            entityStateMachine.mainStateType = new EntityStates.SerializableEntityStateType(mainStateType);

            if (initalStateType == null)
            {
                initalStateType = typeof(EntityStates.SpawnTeleporterState);
            }
            entityStateMachine.initialStateType = new EntityStates.SerializableEntityStateType(initalStateType);

            //Add to NetworkStateMachine so it is networked, as it sounds
            NetworkStateMachine networkMachine = bodyPrefab.GetComponent<NetworkStateMachine>();
            if (networkMachine)
            {
                networkMachine.stateMachines = networkMachine.stateMachines.Append(entityStateMachine).ToArray();
            }

            //Add to the main state machine field of CharacterDeathBehavior for when the character dies.
            //This EntityStateMachine will enter the death state, while other state machines are set to idle
            //The death state is set elsewhere, (likely in the commando clone). It is typically GenericCharacterDeath, but you can set it to whatever you want.
            CharacterDeathBehavior deathBehavior = bodyPrefab.GetComponent<CharacterDeathBehavior>();
            if (deathBehavior)
            {
                deathBehavior.deathStateMachine = entityStateMachine;
            }

            //Add to the main state machine field of SetStateOnHurt for when the character is Stunned/Frozen/etc,
            //This EntityStateMachine will enter the relative state, while other state machines are set to idle.
            SetStateOnHurt setStateOnHurt = bodyPrefab.GetComponent<SetStateOnHurt>();
            if (setStateOnHurt)
            {
                setStateOnHurt.targetStateMachine = entityStateMachine;
            }

            return entityStateMachine;
        }


}}
