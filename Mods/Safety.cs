using GorillaLocomotion;
using StupidTemplate.Classes;
using StupidTemplate.Notifications;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using static StupidTemplate.Classes.RigManager;
using static StupidTemplate.Menu.Main;

namespace StupidTemplate.Mods
{
    public class Safety
    {
        public static VRRig reportRig;
        public static void AntiReport(System.Action<VRRig, Vector3> onReport)
        {
            if (!NetworkSystem.Instance.InRoom) return;

            if (reportRig != null)
            {
                onReport?.Invoke(reportRig, reportRig.transform.position);
                reportRig = null;
                return;
            }

            foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines)
            {
                if (line.linePlayer != NetworkSystem.Instance.LocalPlayer) continue;
                Transform report = line.reportButton.gameObject.transform;

                // Optimization: Replaced costly inline LINQ statement with zero-allocation index loops
                var rigs = GorillaParent.instance.vrrigs;
                int count = rigs.Count;
                for (int i = 0; i < count; i++)
                {
                    VRRig vrrig = rigs[i];
                    if (vrrig == null || vrrig.isLocal) continue;

                    Vector3 rightPos = vrrig.rightHandTransform.position;
                    Vector3 leftPos = vrrig.leftHandTransform.position;

                    float d1 = Vector3.Distance(rightPos, report.position);
                    float d2 = Vector3.Distance(leftPos, report.position);

                    if (d1 < 0.35f || d2 < 0.35f)
                    {
                        onReport?.Invoke(vrrig, report.transform.position);
                    }
                }
            }
        }

        public static float antiReportDelay;
        public static void AntiReportDisconnect()
        {
            AntiReport((vrrig, position) =>
            {
                NetworkSystem.Instance.ReturnToSinglePlayer();

                if (!(Time.time > antiReportDelay)) return;
                antiReportDelay = Time.time + 1f;
                NotifiLib.SendNotification("<color=grey>[</color><color=purple>ANTI-REPORT</color><color=grey>]</color> " + GetPlayerFromVRRig(vrrig).NickName + " attempted to report you, you have been disconnected.");
            });
        }
    }
}
