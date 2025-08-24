using System.Collections.Generic;
using System.Linq;
using DOL.GS.Commands;
using DOL.GS.RealmAbilities;

namespace DOL.GS.PacketHandler.Client.v168
{
    /// <summary>
    /// handles Train clicks from Trainer Window
    /// </summary>
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.TrainHandlerOld, "Handles Player Train", eClientStatus.PlayerInGame)]
    public class PlayerTrainHandlerOld : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            // Old packet for old clients.

            /*uint x = packet.ReadInt();
            uint y = packet.ReadInt();
            int idLine = packet.ReadByte();
            int unk = packet.ReadByte();
            int row = packet.ReadByte();
            int skillIndex = packet.ReadByte();*/
            return;
        }
    }

    /// <summary>
    /// Handles Train clicks from Trainer Window
    /// </summary>
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.TrainHandler, "Handles Player Train", eClientStatus.PlayerInGame)]
    public class PlayerTrainHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            if (!TrainCommandHandler.CanTrain(client))
                return;

            // Specializations.
            uint size = 8;
            long position = packet.Position;
            List<uint> skills = new();
            Dictionary<uint, uint> amounts = new();
            bool stop = false;

            for (uint i = 0; i < size; i++)
            {
                uint code = packet.ReadInt();

                if (!stop)
                {
                    if (code == 0xFFFFFFFF)
                        stop = true;
                    else
                    {
                        if (!skills.Contains(code))
                            skills.Add(code);
                    }
                }
            }

            foreach (uint code in skills)
            {
                uint val = packet.ReadInt();

                if (!amounts.ContainsKey(code) && val > 1)
                    amounts.Add(code, val);
            }

            List<Specialization> specs = client.Player.GetSpecList().Where(e => e.Trainable).ToList();
            uint skillCount = 0;
            List<string> done = new();
            bool trained = false;

            foreach (Specialization spec in specs)
            {
                if (amounts.TryGetValue(skillCount, out uint level) && spec.Level < level)
                {
                    TrainCommandHandler.Train(client, spec, (int) level);
                    trained = true;
                }

                skillCount++;
            }

            // Realm abilities.
            packet.Seek(position + 64, System.IO.SeekOrigin.Begin);
            size = 50;
            amounts.Clear();

            for (uint i = 0; i < size; i++)
            {
                uint val = packet.ReadInt();

                if (val > 0 && !amounts.ContainsKey(i))
                    amounts.Add(i, val);
            }

            if (amounts != null && amounts.Count > 0)
            {
                // Realm abilities.
                var raList = SkillBase.GetClassRealmAbilities(client.Player.CharacterClass.ID).Where(ra => ra is not RR5RealmAbility);

                foreach (var pair in amounts)
                {
                    RealmAbility ra = raList.ElementAtOrDefault((int) pair.Key);

                    if (ra != null)
                    {
                        RealmAbility playerRA = (RealmAbility) client.Player.GetAbility(ra.KeyName);

                        if (playerRA != null && (playerRA.Level >= ra.MaxLevel || playerRA.Level >= pair.Value))
                            continue;

                        int cost = 0;

                        for (int i = playerRA != null ? playerRA.Level : 0; i < pair.Value; i++)
                            cost += ra.CostForUpgrade(i);

                        if (client.Player.RealmSpecialtyPoints < cost)
                        {
                            client.Out.SendMessage($"{ra.Name} costs {cost} realm ability points!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            client.Out.SendMessage("You don't have that many realm ability points left to get this.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            continue;
                        }

                        if (!ra.CheckRequirement(client.Player))
                        {
                            client.Out.SendMessage($"You are not experienced enough to get {ra.Name} now. Come back later.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            continue;
                        }

                        if (playerRA != null)
                            playerRA.Level = (int) pair.Value;
                        else
                        {
                            ra.Level = (int) pair.Value;
                            client.Player.AddRealmAbility(ra, false);
                        }

                        trained = true;
                    }
                }
            }

            if (trained)
                TrainCommandHandler.OnTrained(client);
        }
    }

    /// <summary>
    /// Summon trainer window
    /// </summary>
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.TrainWindowHandler, "Call Player Train Window", eClientStatus.PlayerInGame)]
    public class PlayerTrainWindowHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            if (!TrainCommandHandler.CanTrain(client))
                return;

            client.Out.SendTrainerWindow();
        }
    }
}
