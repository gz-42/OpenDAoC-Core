using System;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&keep",
		ePrivLevel.GM,
		"GMCommands.Keep.Description",
		"GMCommands.Keep.Usage.FastCreate",
		"GMCommands.Keep.Usage.FastCreate.Info",
		"GMCommands.Keep.Usage.Create",
		"GMCommands.Keep.Usage.TowerCreate",
		"GMCommands.Keep.Usage.Remove",
		"GMCommands.Keep.Usage.Name",
		"GMCommands.Keep.Usage.KeepID",
		"GMCommands.Keep.Usage.Level",
		"GMCommands.Keep.Usage.BaseLevel",
		"/keep move {[x,y,z,h] [amount]} - admin only",
		"/keep skintype [0 = any, 1 = old, 2 = new] - force keep to use old or new skins",
		//"GMCommands.Keep.Usage.AddComponent",
		"GMCommands.Keep.Usage.Save",
		"GMCommands.Keep.Usage.AddTeleporter",
		"GMCommands.Keep.Usage.AddBanner",
		"GMCommands.Keep.Usage.Realm",
		"GMCommands.Keep.Usage.Radius")]
	public class KeepCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		protected string TEMP_KEEP_LAST = "TEMP_KEEP_LAST";
		public enum eKeepTypes : int
		{
			DunCrauchonBledmeerFasteCaerBenowyc = 1,
			DunCrimthainnNottmoorFasteCaerBerkstead = 2,
			DunBolgHlidskialfFasteCaerErasleigh = 3,
			DunnGedGlenlockFasteCaerBoldiam = 4,
			DundaBehnnBlendrakeFasteCaerSursbrooke = 5,
			DunScathaigFensalirFasteCaerRenaris = 6,
			DunAilinneArvakrFasteCaerHurbury = 7,
			FortBrolorn = 8,
			BG1_4 = 9,
			ClaimBG5_9 = 10,
			BG5_9 = 11,
			CaerClaret = 12,
			BG10_14 = 13,
			CKBG15_19 = 14,
			BG15_19 = 15,
			CKBG20_24 = 16,
			BG20_24 = 17,
			CKBG25_29 = 18,
			BG25_29 = 19,
			CKBG30_34 = 20,
			BG30_34 = 21,
			CKBG35_39 = 22,
			BG35_39 = 23,
			TBG35_39 = 24,
			TestCKBG40_44 = 25,
			TestBG40_44 = 26,
			TestTBG40_44 = 27,
			CKBG40_44 = 28,
			BG40_44 = 29,
			TBG40_44 = 30,
		}

		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length == 1)
			{
				DisplaySyntax(client);
				return;
			}

			AbstractGameKeep myKeep = client.Player.TempProperties.GetProperty<AbstractGameKeep>(TEMP_KEEP_LAST);
			if (myKeep == null) myKeep = GameServer.KeepManager.GetKeepCloseToSpot(client.Player.CurrentRegionID, client.Player, 10000);
			
			switch (args[1])
			{
				#region FastCreate
				case "fastcreate":
					{
						#region DisplayTemplates
						if (args.Length < 5)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.FastCreate.TypeOfKeep"));
							int i = 1;
							foreach (string str in Enum.GetNames(typeof(eKeepTypes)))
							{
								DisplayMessage(client, "#" + i + ": " + str);
								i++;
							}
							return;
						}
						#endregion DisplayTemplates

						int keepType = 0;
						int keepID = 0;
						string keepName = "New Keep";

						try
						{
							keepType = Convert.ToInt32(args[2]);
							keepID = Convert.ToInt32(args[3]);
							keepName = String.Join(" ", args, 4, args.Length - 4);
						}
						catch
						{
							DisplayMessage(client, "Invalid parameter for Keep Type, Keep ID, or Keep Name");
							return;
						}

						if ((keepID >> 8) != 0 || GameServer.KeepManager.Keeps[keepID] != null)
						{
							DisplayMessage(client, "KeepID must be unused and less than 256.");
							return;
						}

						string createInfo = client.Player.Name + ";" + string.Format("/keep fastcreate {0} {1} {2}", keepType, keepID, keepName);

						GameKeep keep = new GameKeep();
						keep.DBKeep = new DbKeep(createInfo);
						keep.Name = keepName;
						keep.KeepID = (ushort)keepID;
						keep.Level = (byte)ServerProperties.Properties.STARTING_KEEP_LEVEL;
						keep.BaseLevel = 50;
						keep.Realm = client.Player.Realm;
						keep.Region = client.Player.CurrentRegionID;
						keep.X = client.Player.X;
						keep.Y = client.Player.Y;
						keep.Z = client.Player.Z;
						keep.Heading = client.Player.Heading;

						if ((int)keepType < 8)
						{
							keep.KeepType = (AbstractGameKeep.eKeepType)keepType;
						}
						else
						{
							keep.KeepType = 0;
						}

						log.Debug("Keep creation: starting");

						// TODO: Add keep component to list in keep class

						// SQL to grab current keep components from a DB that works.  Replace keepID with the one you want to edit here.
						// Values below taken from Storm with working old style keeps
						// select concat(ID, ', ', skin, ', ', x, ', ', y, ', ', heading, ', ', height, ', ', health) as keepcomponent from keepcomponent where keepid = ### order by id;


						GameKeepComponent keepComp = null;
						
						switch ((eKeepTypes)keepType)
						{
								#region DunCrauchonBledmeerFasteCaerBenowyc
							case eKeepTypes.DunCrauchonBledmeerFasteCaerBenowyc:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 250, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 2, 251, 250, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 1, 4, 250, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 8, 250, 251, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 8, 7, 251, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 2, 8, 252, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 1, 249, 254, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 7, 8, 255, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 7, 249, 1, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 9, 2, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 13, 248, 4, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 2, 249, 7, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 1, 8, 5, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 8, 7, 8, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 8, 250, 8, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 2, 6, 9, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 1, 253, 9, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(17, 9, 3, 10, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(18, 9, 0, 10, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(19, 10, 4, 7, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(20, 14, 2, 9, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion DunCrauchonBledmeerFasteCaerBenowyc
								#region DunCrimthainnNottmoorFasteCaerBerkstead
							case eKeepTypes.DunCrimthainnNottmoorFasteCaerBerkstead:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 1, 4, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 2, 251, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 8, 7, 250, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 8, 250, 250, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 7, 250, 253, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 7, 7, 251, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 1, 7, 254, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 1, 6, 1, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 1, 5, 4, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 249, 0, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 9, 249, 3, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 4, 7, 7, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 9, 0, 8, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 9, 3, 8, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 10, 251, 6, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 9, 253, 8, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(17, 5, 250, 7, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(18, 20, 250, 4, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(19, 13, 249, 6, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion DunCrimthainnNottmoorFasteCaerBerkstead
								#region DunBolgHlidskialfFasteCaerErasleigh
							case eKeepTypes.DunBolgHlidskialfFasteCaerErasleigh:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 253, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 4, 246, 247, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 3, 3, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 9, 248, 253, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 9, 248, 3, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 7, 249, 0, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 248, 6, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 9, 248, 9, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 1, 250, 247, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 248, 250, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 255, 10, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 13, 2, 10, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 3, 6, 8, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 7, 6, 5, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 7, 3, 249, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 2, 4, 252, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 2, 5, 255, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(17, 2, 6, 2, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(18, 10, 250, 8, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(19, 4, 249, 13, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(20, 2, 5, 9, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(21, 2, 252, 10, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(22, 20, 249, 6, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion DunBolgHlidskialfFasteCaerErasleigh
								#region DunnGedGlenlockFasteCaerBoldiam
							case eKeepTypes.DunnGedGlenlockFasteCaerBoldiam:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 3, 250, 246, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 2, 5, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 9, 9, 250, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 6, 254, 247, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 4, 251, 243, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 0, 255, 250, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 4, 8, 246, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 9, 248, 255, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 9, 248, 2, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 2, 249, 5, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 4, 250, 9, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 1, 253, 7, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 1, 0, 8, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 1, 3, 9, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 4, 7, 9, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 1, 8, 253, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 1, 7, 0, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(17, 1, 6, 3, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(18, 1, 5, 6, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(19, 10, 250, 4, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(20, 1, 249, 249, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(21, 13, 248, 252, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(22, 20, 249, 2, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion DunnGedGlenlockFasteCaerBoldiam
								#region DundaBehnnBlendrakeFasteCaerSursbrooke
							case eKeepTypes.DundaBehnnBlendrakeFasteCaerSursbrooke:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 4, 11, 247, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 9, 5, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 0, 252, 250, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 9, 2, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 9, 249, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 4, 245, 250, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 247, 253, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 9, 247, 0, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 9, 12, 251, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 12, 254, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 4, 14, 4, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 4, 248, 7, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 9, 251, 5, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 7, 254, 4, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 2, 10, 5, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 1, 1, 5, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 7, 4, 5, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(17, 9, 8, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(18, 9, 12, 1, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(19, 9, 247, 3, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(20, 13, 7, 6, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(21, 10, 10, 252, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(22, 17, 6, 250, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion DundaBehnnBlendrakeFasteCaerSursbrooke
								#region DunScathaigFensalirFasteCaerRenaris
							case eKeepTypes.DunScathaigFensalirFasteCaerRenaris:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 9, 251, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 9, 4, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 4, 247, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 4, 7, 246, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 9, 249, 252, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 249, 255, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 9, 8, 250, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 9, 8, 253, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 7, 7, 0, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 7, 250, 2, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 13, 8, 3, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 9, 8, 6, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 9, 249, 5, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 9, 249, 8, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 4, 10, 9, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 4, 250, 12, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(17, 9, 6, 10, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(18, 9, 253, 10, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(19, 9, 0, 10, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(20, 9, 3, 10, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(21, 10, 3, 8, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(22, 18, 252, 6, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion DunScathaigFensalirFasteCaerRenaris
								#region DunAilinneArvakrFasteCaerHurbury
							case eKeepTypes.DunAilinneArvakrFasteCaerHurbury:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 250, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 6, 4, 247, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 6, 253, 247, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 4, 8, 243, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 9, 248, 249, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 3, 250, 246, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 3, 7, 246, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 4, 246, 246, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 9, 248, 252, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 7, 249, 255, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 248, 2, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 7, 6, 9, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 9, 9, 247, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 9, 9, 250, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 7, 8, 253, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 9, 9, 0, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 7, 8, 3, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(17, 1, 8, 6, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(18, 2, 249, 8, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(19, 7, 253, 9, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(20, 13, 3, 10, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(21, 3, 7, 9, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(22, 9, 248, 5, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(23, 3, 250, 9, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(24, 9, 0, 10, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(25, 10, 251, 6, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(26, 14, 249, 4, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion DunAilinneArvakrFasteCaerHurbury
								#region FortBrolorn
							case eKeepTypes.FortBrolorn:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 3, 5, 255, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 3, 251, 255, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 3, 250, 3, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 3, 6, 3, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 8, 2, 10, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 9, 253, 9, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 252, 254, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 9, 3, 7, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 9, 255, 254, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 6, 251, 6, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 6, 5, 6, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 2, 6, 0, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 1, 250, 2, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 9, 2, 254, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 8, 254, 10, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 19, 1, 11, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion FortBrolorn
								#region BG1_4
							case eKeepTypes.BG1_4:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 1, 251, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 2, 4, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 4, 7, 245, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 4, 247, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 9, 249, 251, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 8, 249, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 7, 7, 252, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 7, 250, 254, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 8, 255, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 249, 1, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 4, 10, 2, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 4, 250, 5, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 9, 6, 3, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 9, 253, 3, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 9, 3, 3, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 9, 0, 3, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion BG1_4
								#region ClaimBG5_9
							case eKeepTypes.ClaimBG5_9:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 5, 5, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 5, 251, 249, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 7, 251, 255, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 9, 250, 252, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 9, 6, 250, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 7, 5, 253, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 6, 0, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 5, 5, 3, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 9, 4, 4, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 1, 252, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 2, 2, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 9, 1, 4, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 9, 254, 4, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 5, 251, 3, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 9, 250, 2, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 19, 255, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion ClaimBG5_9
								#region BG5_9
							case eKeepTypes.BG5_9:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 1, 251, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 2, 4, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 4, 7, 245, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 4, 247, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 9, 249, 251, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 8, 249, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 7, 7, 252, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 7, 250, 254, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 8, 255, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 249, 1, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 4, 10, 2, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 4, 250, 5, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 9, 6, 3, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 9, 253, 3, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 9, 3, 3, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 9, 0, 3, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion BG5_9
								#region CaerClaret
							case eKeepTypes.CaerClaret:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 252, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 4, 4, 249, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 4, 250, 252, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 9, 252, 255, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 9, 252, 2, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 9, 5, 253, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 5, 0, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 4, 253, 6, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 9, 0, 4, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 4, 7, 3, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 3, 4, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion CaerClaret
								#region BG10_14
							case eKeepTypes.BG10_14:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 1, 251, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 2, 4, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 4, 7, 245, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 4, 247, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 9, 249, 251, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 8, 249, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 7, 7, 252, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 7, 250, 254, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 8, 255, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 249, 1, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 4, 10, 2, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 4, 250, 5, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 9, 6, 3, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 9, 253, 3, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 9, 3, 3, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 9, 0, 3, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion BG10_14
								#region CKBG15_19
							case eKeepTypes.CKBG15_19:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 4, 247, 250, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 4, 007, 247, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 4, 250, 010, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 4, 010, 007, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 0, 254, 251, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 2, 004, 250, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 1, 251, 250, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 1, 253, 008, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 2, 006, 008, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 000, 009, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 003, 009, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 1, 007, 251, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 1, 250, 006, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 2, 250, 253, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 2, 007, 004, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 0, 004, 006, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 9, 007, 254, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(17, 9, 007, 001, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(18, 9, 250, 003, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(19, 9, 250, 000, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion CKBG15_19
								#region BG15_19
							case eKeepTypes.BG15_19:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 1, 251, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 2, 004, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 4, 007, 245, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 4, 247, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 9, 249, 251, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 008, 249, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 7, 007, 252, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 7, 250, 254, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 008, 255, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 249, 001, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 4, 010, 002, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 4, 250, 005, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 9, 006, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 9, 253, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 9, 003, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 9, 000, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion BG15_19
								#region CKBG20_24
							case eKeepTypes.CKBG20_24:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 253, 251, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 9, 3, 250, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 9, 250, 250, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 9, 6, 250, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 4, 246, 251, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 4, 9, 248, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 248, 254, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 9, 10, 255, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 9, 10, 002, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 248, 001, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 248, 004, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 9, 10, 252, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 9, 10, 5, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 9, 248, 7, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 4, 249, 11, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 4, 12, 8, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 9, 255, 9, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(17, 9, 5, 9, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(18, 9, 8, 9, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(19, 9, 252, 9, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(20, 9, 2, 9, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(21, 10, 253, 005, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion CKBG20_24
								#region BG20_24
							case eKeepTypes.BG20_24:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 1, 251, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 2, 004, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 4, 007, 245, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 4, 247, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 9, 249, 251, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 008, 249, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 7, 007, 252, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 7, 250, 254, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 008, 255, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 249, 001, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 4, 010, 002, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 4, 250, 005, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 9, 006, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 9, 253, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 9, 003, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 9, 000, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion BG20_24
								#region CKBG25_29
							case eKeepTypes.CKBG25_29:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 9, 251, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 9, 004, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 9, 003, 007, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 9, 000, 007, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 9, 246, 005, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 011, 003, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 4, 247, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 4, 007, 246, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 4, 247, 009, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 4, 013, 006, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 1, 249, 252, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 1, 248, 255, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 1, 247, 002, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 2, 008, 250, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 2, 009, 253, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 2, 010, 000, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(17, 2, 009, 007, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(18, 2, 253, 007, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(19, 1, 006, 007, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(20, 1, 250, 007, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(21, 10, 006, 253, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion CKBG25_29
								#region BG25_29
							case eKeepTypes.BG25_29:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 1, 251, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 2, 004, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 4, 007, 245, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 4, 247, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 9, 249, 251, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 008, 249, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 7, 007, 252, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 7, 250, 254, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 008, 255, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 249, 001, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 4, 010, 002, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 4, 250, 005, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 9, 006, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 9, 253, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 9, 003, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 9, 000, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion BG25_29
								#region CKBG30_34
							case eKeepTypes.CKBG30_34:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 255, 250, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 9, 005, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 9, 252, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 9, 249, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 4, 008, 247, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 4, 245, 250, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 247, 253, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 9, 247, 003, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 9, 247, 000, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 009, 251, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 009, 254, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 1, 008, 001, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 1, 007, 004, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 2, 248, 006, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 3, 249, 007, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 3, 006, 007, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 7, 252, 007, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(17, 7, 005, 007, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(18, 9, 255, 008, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(19, 9, 002, 008, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(20, 10, 250, 004, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion CKBG30_34
								#region BG30_34
							case eKeepTypes.BG30_34:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 1, 251, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 2, 004, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 4, 007, 245, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 4, 247, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 9, 249, 251, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 008, 249, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 7, 007, 252, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 7, 250, 254, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 008, 255, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 249, 001, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 4, 010, 002, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 4, 250, 005, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 9, 006, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 9, 253, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 9, 003, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 9, 000, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion BG30_34
								#region CKBG35_39
							case eKeepTypes.CKBG35_39:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 9, 251, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 9, 004, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 4, 007, 246, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 4, 247, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 9, 008, 250, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 249, 252, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 9, 249, 255, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 9, 008, 253, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 008, 003, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 7, 250, 002, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 7, 007, 000, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 9, 249, 005, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 10, 253, 253, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 9, 008, 006, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 9, 249, 008, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 4, 010, 009, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(17, 4, 250, 012, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(18, 9, 253, 010, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(19, 9, 006, 010, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(20, 7, 003, 009, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(21, 7, 000, 009, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion CKBG35_39
								#region BG35_39
							case eKeepTypes.BG35_39:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 1, 251, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 2, 004, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 4, 007, 245, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 4, 247, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 9, 249, 251, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 008, 249, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 7, 007, 252, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 7, 250, 254, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 008, 255, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 249, 001, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 4, 010, 002, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 4, 250, 005, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 9, 006, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 9, 253, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 9, 003, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 9, 000, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion BG35_39
								#region TBG35_39
							case eKeepTypes.TBG35_39:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 11, 253, 004, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion TBG35_39
								#region TestCKBG40_44
							case eKeepTypes.TestCKBG40_44:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 004, 247, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 9, 001, 246, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 9, 251, 246, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 9, 248, 246, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 7, 254, 247, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 4, 244, 247, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 5, 010, 247, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 9, 246, 250, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 9, 246, 253, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 011, 248, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 011, 254, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 9, 011, 001, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 7, 247, 000, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 7, 010, 251, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 4, 013, 004, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 6, 009, 007, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 6, 249, 007, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(17, 3, 006, 008, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(18, 3, 252, 008, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(19, 9, 255, 009, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(20, 9, 005, 009, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(21, 7, 002, 008, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(22, 4, 248, 007, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(23, 2, 247, 003, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(24, 10, 254, 252, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion TestCKBG40_44
								#region TestBG40_44
							case eKeepTypes.TestBG40_44:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 1, 251, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 2, 004, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 4, 007, 245, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 4, 247, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 9, 249, 251, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 008, 249, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 7, 007, 252, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 7, 250, 254, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 008, 255, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 249, 001, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 4, 010, 002, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 4, 250, 005, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 9, 006, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 9, 253, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 9, 003, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 9, 000, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion TestBG40_44
								#region TestTBG40_44
							case eKeepTypes.TestTBG40_44:
								{
									keep.KeepComponents.Add(keepComp);
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 11, 253, 004, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keepComp = new GameKeepComponent();
									break;
								}
								#endregion TestTBG40_44
								#region CKBG40_44
							case eKeepTypes.CKBG40_44:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 004, 247, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 9, 001, 246, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 9, 251, 246, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 9, 248, 246, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 7, 254, 247, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 4, 244, 247, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 5, 010, 247, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 9, 246, 250, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 9, 246, 253, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 011, 248, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 011, 254, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 9, 011, 001, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 7, 247, 000, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 7, 010, 251, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 4, 013, 004, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 6, 009, 007, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 6, 249, 007, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(17, 3, 006, 008, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(18, 3, 252, 008, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(19, 9, 255, 009, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(20, 9, 005, 009, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(21, 7, 002, 008, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(22, 4, 248, 007, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(23, 2, 247, 003, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(24, 10, 254, 252, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion CKBG40_44
								#region BG40_44
							case eKeepTypes.BG40_44:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 0, 254, 249, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(1, 1, 251, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(2, 2, 004, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(3, 4, 007, 245, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(4, 4, 247, 248, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(5, 9, 249, 251, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(6, 9, 008, 249, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(7, 7, 007, 252, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(8, 7, 250, 254, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(9, 9, 008, 255, 3, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(10, 9, 249, 001, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(11, 4, 010, 002, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(12, 4, 250, 005, 1, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(13, 9, 006, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(14, 9, 253, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(15, 9, 003, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(16, 9, 000, 003, 2, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion BG40_44
								#region TBG40_44
							case eKeepTypes.TBG40_44:
								{
									keepComp = new GameKeepComponent();
									keepComp.LoadFromDatabase(new DbKeepComponent(0, 11, 253, 004, 0, 0, 3200, keep.KeepID, createInfo), keep);
									keep.KeepComponents.Add(keepComp);
									break;
								}
								#endregion TBG40_44
								#region Default
							default:
								DisplayMessage(client, "Wrong type of keep");
								return;
								#endregion Default
						}

						log.Debug("Keep creation: used keep type " + ((eKeepTypes)keepType));

						client.Player.TempProperties.SetProperty(TEMP_KEEP_LAST, keep);
						foreach (GameKeepComponent comp in keep.KeepComponents)
						{
							if (comp.InternalID != null)
								DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.FastCreate.CompCreated", comp.InternalID, comp.Keep.KeepID));

							comp.Health = comp.MaxHealth;
						}
						DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.FastCreate.KeepCreated"));

						log.Debug("Keep creation: check of components complete");

						foreach (GamePlayer otherPlayer in ClientService.GetPlayersOfRegion(client.Player.CurrentRegion))
						{
							otherPlayer.Out.SendKeepInfo(keep);

							foreach (GameKeepComponent keepComponent in keep.KeepComponents)
								otherPlayer.Out.SendKeepComponentInfo(keepComponent);
						}

						log.Debug("Keep creation: complete, saving");

						keep.SaveIntoDatabase();
						break;
					}
					#endregion FastCreate
				#region TowerCreate
				case "towercreate":
					{
						if (args.Length < 5)
						{
							DisplaySyntax(client);
							return;
						}

						int keepid = -1;
						if (!int.TryParse(args[2], out keepid))
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.TowerCreate.InvalidKeepID"));
							return;
						}

						if (GameServer.KeepManager.GetKeepByID(keepid) != null)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.TowerCreate.KeepIDExists", keepid));
							return;
						}

						// Most //
						// Since the KeepManager consider a KeepID higher than 255 as a Tower KeepID
						// We must check that the client is not trying to create a tower with a lower KeepID
						if ((keepid >> 8) == 0)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.TowerCreate.WrongKeepID", keepid));
							return;
						}

						byte baseLevel = 50;
						if (!byte.TryParse(args[3], out baseLevel))
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.TowerCreate.InvalidBaseLev"));
							return;
						}

						string keepName = String.Join(" ", args, 4, args.Length - 4);

						string createInfo = client.Player.Name + ";" + string.Format("/keep towercreate {0} {1} {2}", keepid, baseLevel, keepName);

						DbKeep keep = new DbKeep(createInfo);
						keep.Name = keepName;
						keep.KeepID = keepid;
						keep.Level = 0;
						keep.Region = client.Player.CurrentRegionID;
						keep.X = client.Player.X;
						keep.Y = client.Player.Y;
						keep.Z = client.Player.Z;
						keep.Heading = client.Player.Heading;
						keep.BaseLevel = baseLevel;
						GameServer.Database.AddObject(keep);

						DbKeepComponent towerComponent = new DbKeepComponent(0, (int)GameKeepComponent.eComponentSkin.Tower, 0, 0, 0, 0, 3200, keep.KeepID, client.Player.Name + ";/keep towercreate");
						GameServer.Database.AddObject(towerComponent);

						GameKeepTower k = new GameKeepTower();
						k.Load(keep);
						new GameKeepComponent().LoadFromDatabase(towerComponent);
						DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.TowerCreate.CreatedSaved"));

						//send the creation packets
						foreach (GamePlayer otherPlayer in ClientService.GetPlayersOfRegion(client.Player.CurrentRegion))
						{
							otherPlayer.Out.SendKeepInfo(k);
							otherPlayer.Out.SendKeepComponentUpdate(k, false);

							foreach (GameKeepComponent keepComponent in k.KeepComponents)
							{
								otherPlayer.Out.SendKeepComponentInfo(keepComponent);
								otherPlayer.Out.SendKeepComponentDetailUpdate(keepComponent);
							}
						}

						break;
					}
					#endregion TowerCreate
				#region Create
				case "create":
					{
						if (args.Length < 6)
						{
							DisplaySyntax(client);
							return;
						}

						int keepid = 0;
						try
						{
							keepid = Convert.ToInt32(args[2]);
						}
						catch
						{
							DisplaySyntax(client);
							return;
						}

						if (GameServer.KeepManager.GetKeepByID(keepid) != null)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.TowerCreate.KeepIDExists", keepid));
							return;
						}

						// Most //
						// Since the KeepManager consider a KeepID lower than 256 as a keep KeepID
						// We must check that the client is not trying to create a keep with a higher KeepID
						if ((keepid >> 8) != 0)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.TowerCreate.WrongKeepID", keepid));
							return;
						}

						byte baselevel = 0;
						try
						{
							baselevel = Convert.ToByte(args[3]);
						}
						catch
						{
							DisplaySyntax(client);
							return;
						}

						int radius = 0;
						try
						{
							radius = Convert.ToInt32(args[4]);
						}
						catch
						{
							DisplaySyntax(client);
							return;
						}

						string keepName = String.Join(" ", args, 5, args.Length - 5);

						string createInfo = client.Player.Name + ";" + string.Format("/keep create {0} {1} {2} {3}", keepid, baselevel, radius, keepName);

						DbKeep keep = new DbKeep(createInfo);
						keep.Name = keepName;
						keep.KeepID = keepid;
						keep.Level = 0;
						keep.Region = client.Player.CurrentRegionID;
						keep.X = client.Player.X;
						keep.Y = client.Player.Y;
						keep.Z = client.Player.Z;
						keep.Heading = client.Player.Heading;
						keep.BaseLevel = baselevel;
						GameServer.Database.AddObject(keep);

						GameKeep k = new GameKeep();
						k.Load(keep);

						if (radius > 0)
							k.Area.ChangeRadius(radius);

						foreach (GameDoorBase door in client.Player.GetDoorsInRadius(3000))
						{
							door.RemoveFromWorld();
							GameKeepDoor d = new GameKeepDoor();
							d.CurrentRegionID = (ushort)keep.Region;
							d.Name = door.Name;
							d.Heading = (ushort)door.Heading;
							d.X = door.X;
							d.Y = door.Y;
							d.Z = door.Z;
							d.Level = 0;
							d.Model = 0xFFFF;
							d.DoorId = door.DoorId;
							d.State = eDoorState.Closed;

							DoorMgr.RegisterDoor(d);
							d.AddToWorld();

							d.Component = new GameKeepComponent();
							d.Component.Keep = k;
							d.Component.Keep.Doors.Add(d.DoorId.ToString(), d);

							d.Health = d.MaxHealth;
							d.StartHealthRegeneration();

							(door as GameObject).Delete();
						}
						client.Player.TempProperties.SetProperty(TEMP_KEEP_LAST, k);
						DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.FastCreate.KeepCreated"));

						//send the creation packets
						foreach (GamePlayer otherPlayer in ClientService.GetPlayersOfRegion(client.Player.CurrentRegion))
						{
							otherPlayer.Out.SendKeepInfo(k);

							foreach (GameKeepComponent keepComponent in k.KeepComponents)
								otherPlayer.Out.SendKeepComponentInfo(keepComponent);
						}

                        GameServer.KeepManager.RegisterKeep(k.KeepID, k);
						break;
					}
					#endregion Create
				#region Remove
				case "remove":
					{
						KeepArea karea = null;
						foreach (AbstractArea area in client.Player.CurrentAreas)
						{
							if (area is KeepArea)
							{
								karea = area as KeepArea;
								break;
							}
						}

						if (karea == null)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Remove.YourNotInAKeepArea"));
							return;
						}

						karea.Keep.Remove(karea);

						DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Remove.KeepUnloaded"));
						break;
					}
					#endregion Remove
				#region Name
				case "name":
					{
						if (args.Length < 3)
						{
							DisplaySyntax(client);
							return;
						}
						if (myKeep == null)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Remove.MustCreateKeepFirst"));
							return;
						}
						myKeep.Name = String.Join(" ", args, 2, args.Length - 2);
						DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Remove.YouChangeKeepName", myKeep.Name));
						break;
					}
					#endregion Name
				#region KeepID
				case "keepid":
					{
						if (args.Length < 3)
						{
							DisplaySyntax(client);
							return;
						}
						if (myKeep == null)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Remove.MustCreateKeepFirst"));
							return;
						}
						int keepid = 0;
						try
						{
							keepid = Convert.ToInt32(args[2]);
						}
						catch
						{
							DisplaySyntax(client);
							return;
						}
						myKeep.KeepID = (ushort)keepid;
						DisplayMessage(client, "You change the id of the current keep to " + keepid);
						break;
					}
					#endregion KeepID
				case "idnext":
					{
						if (myKeep == null)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Remove.MustCreateKeepFirst"));
							return;
						}
						
						myKeep.KeepID++;
						DisplayMessage(client, "You change the id of the current keep to " + myKeep.KeepID);
						break;
					}
				case "idprev":
					{
						if (myKeep == null)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Remove.MustCreateKeepFirst"));
							return;
						}
						
						myKeep.KeepID--;
						DisplayMessage(client, "You change the id of the current keep to " + myKeep.KeepID);
						break;
					}
				#region Level
				case "level":
					{
						if (args.Length < 3)
						{
							DisplaySyntax(client);
							return;
						}
						if (myKeep == null)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Remove.MustCreateKeepFirst"));
							return;
						}
						byte keepLevel = 0;
						try
						{
							keepLevel = Convert.ToByte(args[2]);
						}
						catch
						{
							DisplaySyntax(client);
							return;
						}
						myKeep.ChangeLevel(keepLevel);
						DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Level.YouChangeKeepLevel", keepLevel));
						break;
					}
					#endregion Level
				#region BaseLevel
				case "baselevel":
					{
						if (args.Length < 3)
						{
							DisplaySyntax(client);
							return;
						}
						if (myKeep == null)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Remove.MustCreateKeepFirst"));
							return;
						}
						byte keepLevel = 0;
						try
						{
							keepLevel = Convert.ToByte(args[2]);
						}
						catch
						{
							DisplaySyntax(client);
							return;
						}
						myKeep.DBKeep.BaseLevel = keepLevel;
						myKeep.ChangeLevel(myKeep.Level);
						DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.BaseLevel.YouChangeBaseLev", keepLevel));

						break;
					}
					#endregion BaseLevel
				#region Realm
				case "realm":
					{
						if (args.Length < 3)
						{
							DisplaySyntax(client);
							return;
						}
						if (myKeep == null)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Remove.MustCreateKeepFirst"));
							return;
						}
						eRealm realm = eRealm.None;
						try
						{
							realm = (eRealm)Convert.ToByte(args[2]);
						}
						catch
						{
							DisplaySyntax(client);
							return;
						}
						myKeep.Reset(realm);
						DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Realm.YouChangeKeepRealm", GlobalConstants.RealmToName(realm)));
						break;
					}
					#endregion Realm
				#region Radius
				case "radius":
					{
						if (args.Length < 3)
						{
							DisplaySyntax(client);
							return;
						}
						if (myKeep == null)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Remove.MustCreateKeepFirst"));
							return;
						}
						int radius = 0;
						try
						{
							radius = Convert.ToInt32(args[2]);
						}
						catch
						{
							DisplaySyntax(client);
							return;
						}
						myKeep.Area.ChangeRadius(radius);
						DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Radius.YouChangeKeepRadius", radius));
						break;
					}
					#endregion Radius
				#region Save
				case "save":
					{
						if (myKeep == null)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Remove.MustCreateKeepFirst"));
							return;
						}
						myKeep.SaveIntoDatabase();
						DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Save.KeepSavedInDatabase"));
						break;
					}
					#endregion Save
				#region AddTeleport
				case "addteleporter":
					{
						GameKeepComponent component = client.Player.TargetObject as GameKeepComponent;
						if (component != null)
						{
							DbKeepPosition pos = PositionMgr.CreatePosition(typeof(FrontiersPortalStone), 0, client.Player, Guid.NewGuid().ToString(), component);
							PositionMgr.AddPosition(pos);
							PositionMgr.FillPositions();
						}
						else
						{
							FrontiersPortalStone stone = new FrontiersPortalStone();
							stone.CurrentRegion = client.Player.CurrentRegion;
							stone.X = client.Player.X;
							stone.Y = client.Player.Y;
							stone.Z = client.Player.Z;
							stone.Heading = client.Player.Heading;
							stone.SaveIntoDatabase();
							stone.AddToWorld();
						}
						DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.AddTeleport.StoneAdded"));
						break;
					}
					#endregion AddTeleport
				#region AddBanner
				case "addbanner":
					{
						GameKeepBanner.eBannerType bannerType = GameKeepBanner.eBannerType.Realm;
						if (args.Length > 2)
						{
							switch (args[2].ToLower())
							{
									case "realm": bannerType = GameKeepBanner.eBannerType.Realm; break;
									case "guild": bannerType = GameKeepBanner.eBannerType.Guild; break;
									default: return;
							}
						}

						GameKeepComponent component = client.Player.TargetObject as GameKeepComponent;
						if (component != null)
						{
							DbKeepPosition pos = PositionMgr.CreatePosition(typeof(GameKeepBanner), 0, client.Player, Guid.NewGuid().ToString(), component);
							pos.TemplateType = (int)bannerType;
							GameServer.Database.SaveObject(pos);
							PositionMgr.AddPosition(pos);
							PositionMgr.FillPositions();
						}
						else
						{
							GameKeepBanner banner = new GameKeepBanner();
							banner.BannerType = bannerType;
							banner.CurrentRegion = client.Player.CurrentRegion;
							banner.X = client.Player.X;
							banner.Y = client.Player.Y;
							banner.Z = client.Player.Z;
							banner.Heading = client.Player.Heading;
							banner.SaveIntoDatabase();

							foreach (AbstractArea area in banner.CurrentAreas)
							{
								if (area is KeepArea)
								{
									AbstractGameKeep keep = (area as KeepArea).Keep;
									banner.Component = new GameKeepComponent();
									banner.Component.Keep = keep;
									banner.Component.Keep.Banners.Add(banner.InternalID, banner);
									break;
								}
							}

							if (banner.Component.Keep.Guild != null)
								banner.ChangeGuild();
							else banner.ChangeRealm();
							banner.AddToWorld();
						}
						DisplayMessage(client, "Banner added!");
						break;
					}
					#endregion Addbanner
				#region Move
				case "move":
					{
						if (client.Account.PrivLevel < 3)
							return;

						if (myKeep == null)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Remove.MustCreateKeepFirst"));
							return;
						}

						if (args.Length < 3)
						{
							// simple move to player location
							myKeep.X = client.Player.X;
							myKeep.Y = client.Player.Y;
							myKeep.Z = client.Player.Z;
							myKeep.Heading = client.Player.Heading;
						}
						else if (args.Length < 4)
						{
							DisplayMessage(client, "/keep move [direction] [amount]");
							return;
						}
						else
						{
							string direction = args[2];
							int amount = Convert.ToInt32(args[3]);

							switch (direction.ToLower())
							{
								case "x":
									myKeep.X += amount;
									break;

								case "y":
									myKeep.Y += amount;
									break;

								case "z":
									myKeep.Z += amount;
									break;

								case "h":

									if (amount < 0 && myKeep.Heading - Math.Abs(amount) < 0)
									{
										int diff = myKeep.Heading - Math.Abs(amount);
										myKeep.Heading = (ushort)(4095 + diff);
									}
									else
									{
										myKeep.Heading += (ushort)amount;
									}

									if (myKeep.Heading > 4095)
									{
										myKeep.Heading = (ushort)(myKeep.Heading - 4095);
									}

									break;

								default:
									break;
							}
						}

						foreach (GamePlayer otherPlayer in ClientService.GetPlayersOfRegion(client.Player.CurrentRegion))
						{
							otherPlayer.Out.SendKeepRemove(myKeep);
							otherPlayer.Out.SendKeepInfo(myKeep);

							foreach (GameKeepComponent keepComponent in myKeep.KeepComponents)
							{
								otherPlayer.Out.SendKeepComponentInfo(keepComponent);
								otherPlayer.Out.SendKeepComponentDetailUpdate(keepComponent);
							}
						}

						DisplayMessage(client, "Keep moved.  Don't forget to '/keep save' your changes.");
						break;
					}
				#endregion Move
				#region SkinType
				case "skintype":
					{
						if (myKeep == null)
						{
							DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Keep.Remove.MustCreateKeepFirst"));
							return;
						}

						try
						{
							byte skinType = Convert.ToByte(args[2]);

							if (skinType < 3)
							{
								myKeep.DBKeep.KeepSkinType = (EKeepSkinType)skinType;
								DisplayMessage(client, "Keep skin type changed to " + myKeep.DBKeep.KeepSkinType + ". Don't forget to '/keep save' your changes.");
							}
							else
							{
								DisplayMessage(client, "/keep skintype [0 = any, 1 = old, 2 = new]");
							}
						}
						catch
						{
							DisplayMessage(client, "/keep skintype [0 = any, 1 = old, 2 = new]");
						}

						break;
					}
				#endregion Move

					#region Default
				default:
					{
						DisplaySyntax(client);
						break;
					}
					#endregion Default
			}
		}
	}
}
