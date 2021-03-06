﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using RimWorld;

namespace RT_SolarFlareShield
{
	public class CompProperties_RTSolarFlareShield : CompProperties
	{
		public float shieldingPowerDrain = 0.0f;
		public float heatingPerTick = 0.0f;
		public float rotatorSpeedActive = 10.0f;
		public float rotatorSpeedIdle = 0.5f;

		public CompProperties_RTSolarFlareShield()
		{
			compClass = typeof(CompRTSolarFlareShield);
		}
	}

	public class CompRTSolarFlareShield : ThingComp
	{
		public CompProperties_RTSolarFlareShield properties
		{
			get
			{
				return (CompProperties_RTSolarFlareShield)props;
			}
		}
		
		private CompPowerTrader compPowerTrader;
		private MapComponent_ShieldCoordinator coordinator;
		private float rotatorAngle = (float)Rand.Range(0, 360);

		public override void PostSpawnSetup(bool respawningAfterLoad)
		{
			base.PostSpawnSetup(respawningAfterLoad);
			compPowerTrader = parent.TryGetComp<CompPowerTrader>();
			if (compPowerTrader == null)
			{
				Log.Error("[RT Solar Flare Shield]: Could not get CompPowerTrader of " + parent);
			}
			coordinator = parent.Map.GetShieldCoordinator();
			coordinator.hasAnyShield = true;
		}

		public override void PostDeSpawn(Map map)
		{
			coordinator.hasAnyShield = false;
			coordinator.hasActiveShield = false;
			base.PostDeSpawn(map);
		}

		public override string CompInspectStringExtra()
		{
			return "CompRTSolarFlareShield_FlareProtection".Translate();
		}

		public override void CompTick()
		{
			SolarFlareShieldTick(1);
		}

		public override void PostDraw()
		{       // Thanks Skullywag!
			Vector3 vector = new Vector3(2.0f, 2.0f, 2.0f);
			vector.y = Altitudes.AltitudeFor(AltitudeLayer.VisEffects);
			Matrix4x4 matrix = default(Matrix4x4);
			matrix.SetTRS(
				parent.DrawPos + Altitudes.AltIncVect,
				Quaternion.AngleAxis(rotatorAngle, Vector3.up),
				vector);
			Graphics.DrawMesh(MeshPool.plane10, matrix, Resources.rotatorTexture, 0);
		}

		private void SolarFlareShieldTick(int tickAmount)
		{
			if ((Find.TickManager.TicksGame) % tickAmount == 0)
			{
				if (compPowerTrader == null || compPowerTrader.PowerOn)
				{
					coordinator.hasActiveShield = true;
					GameCondition gameCondition =
						Find.World.GameConditionManager.GetActiveCondition(GameConditionDefOf.SolarFlare);
					if (gameCondition != null)
					{
						compPowerTrader.PowerOutput = -properties.shieldingPowerDrain;
						rotatorAngle += properties.rotatorSpeedActive * tickAmount;
						RoomGroup roomGroup = parent.GetRoomGroup();
						if (roomGroup != null && !roomGroup.UsesOutdoorTemperature)
						{
							roomGroup.Temperature += properties.heatingPerTick * tickAmount;
						}
						if ((Find.TickManager.TicksGame) % (5 * tickAmount) == 0)
						{
							foreach (Building building in parent.Map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial))
							{
								Building_CommsConsole console = building as Building_CommsConsole;
								if (console != null)
								{
									CompPowerTrader consoleCompPowerTrader = console.TryGetComp<CompPowerTrader>();
									if (consoleCompPowerTrader != null)
									{
										consoleCompPowerTrader.PowerOn = false;
									}
								}
							}
						}
					}
					else
					{
						compPowerTrader.PowerOutput = -compPowerTrader.Props.basePowerConsumption;
						rotatorAngle += properties.rotatorSpeedIdle * tickAmount;
					}
				}
				else
				{
					coordinator.hasActiveShield = false;
				}
			}
		}
	}
}
