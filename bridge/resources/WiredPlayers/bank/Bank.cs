﻿using GTANetworkAPI;
using WiredPlayers.database;
using WiredPlayers.globals;
using WiredPlayers.model;
using System.Collections.Generic;
using System;

namespace WiredPlayers.bank
{
    public class Bank : Script
    {
        [RemoteEvent("executeBankOperation")]
        public void ExecuteBankOperationEvent(Client player, int operation, int amount, String targetName)
        {
            String response = String.Empty;
            int bank = NAPI.Data.GetEntitySharedData(player, EntityData.PLAYER_BANK);
            int money = NAPI.Data.GetEntitySharedData(player, EntityData.PLAYER_MONEY);
            String name = NAPI.Data.GetEntityData(player, EntityData.PLAYER_NAME);

            // Checking that amount is valid
            if (amount > 0)
            {
                switch (operation)
                {
                    case Constants.OPERATION_WITHDRAW:
                        if (bank >= amount)
                        {
                            bank -= amount;
                            money += amount;
                            NAPI.Data.SetEntitySharedData(player, EntityData.PLAYER_BANK, bank);
                            NAPI.Data.SetEntitySharedData(player, EntityData.PLAYER_MONEY, money);
                            Database.LogPayment("ATM", name, "Retiro", amount);
                        }
                        else
                        {
                            response = Messages.ERR_BANK_NOT_ENOUGH_MONEY;
                        }
                        break;
                    case Constants.OPERATION_DEPOSIT:
                        if (money >= amount)
                        {
                            bank += amount;
                            money -= amount;
                            NAPI.Data.SetEntitySharedData(player, EntityData.PLAYER_BANK, bank);
                            NAPI.Data.SetEntitySharedData(player, EntityData.PLAYER_MONEY, money);
                            Database.LogPayment(name, "ATM", "Depósito", amount);
                        }
                        else
                        {
                            bank += money;
                            NAPI.Data.SetEntitySharedData(player, EntityData.PLAYER_BANK, bank);
                            NAPI.Data.SetEntitySharedData(player, EntityData.PLAYER_MONEY, 0);
                            Database.LogPayment(name, "ATM", "Depósito", money);
                        }
                        break;
                    case Constants.OPERATION_TRANSFER:
                        if (bank < amount)
                        {
                            response = Messages.ERR_BANK_NOT_ENOUGH_MONEY;
                        }
                        else
                        {
                            if (Database.FindCharacter(targetName) == true)
                            {
                                Client target = NAPI.Pools.GetAllPlayers().Find(x => x.Name == targetName);
                                if (target == player)
                                {
                                    response = Messages.ERR_TRANSFER_MONEY_OWN;
                                }
                                else
                                {
                                    if (target != null && NAPI.Data.HasEntityData(target, EntityData.PLAYER_PLAYING) == true)
                                    {
                                        int targetBank = NAPI.Data.GetEntitySharedData(target, EntityData.PLAYER_BANK);
                                        targetBank += amount;
                                        bank -= amount;
                                        NAPI.Data.SetEntitySharedData(player, EntityData.PLAYER_BANK, bank);
                                        NAPI.Data.SetEntitySharedData(target, EntityData.PLAYER_BANK, targetBank);
                                    }
                                    else
                                    {
                                        bank -= amount;
                                        NAPI.Data.SetEntitySharedData(player, EntityData.PLAYER_BANK, bank);
                                        Database.TransferMoneyToPlayer(targetName, amount);
                                    }
                                    Database.LogPayment(name, targetName, "Transferencia", amount);
                                }
                            }
                            else
                            {
                                response = Messages.ERR_BANK_ACCOUNT_NOT_FOUND;
                            }
                        }
                        break;
                    case Constants.OPERATION_BALANCE:
                        break;
                }
            }
            else
            {
                response = Messages.ERR_BANK_GENERAL_ERROR;
            }

            NAPI.ClientEvent.TriggerClientEvent(player, "bankOperationResponse", response);
        }

        [RemoteEvent("loadPlayerBankBalance")]
        public void LoadPlayerBankBalanceEvent(Client player)
        {
            List<BankOperationModel> operations = Database.GetBankOperations(player.Name, 1, Constants.MAX_BANK_OPERATIONS);
            NAPI.ClientEvent.TriggerClientEvent(player, "showPlayerBankBalance", NAPI.Util.ToJson(operations), player.Name);
        }
    }
}
