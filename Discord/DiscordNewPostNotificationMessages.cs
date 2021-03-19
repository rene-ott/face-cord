using System;
using System.Collections.Generic;

namespace FaceCord.Discord
{
    public static class DiscordNewPostNotificationMessages
    {
        private static IList<string> Messages { get; } = new List<string>
        {
            "Uus postitus on kohal - Aamen!",
            "Lugege uut postitust, enne kui globalistid selle eemaldavad!",
            "Kõigi maade konservatiivid ühinege uue postituse lugemiseks!",
            "Ravige enda haigust nimega liberalism, minu uue postitusega!",
            "Võitleme koos Sorose vastu koos minu postitusega!",
            "Kuradi papid, tulge loeme koos mu uut postitust!",
            "Kiirelt, uus postitus saadavalt!",
            "Deus Vult!",
            "Lõin 95 uut teesi oma Facebooki seinale!"
        };

        public static string GetMessage()
        {
            return Messages[new Random().Next(0, Messages.Count - 1)];
        }
    }
}
