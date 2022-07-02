

namespace Discord.InviteFilter.Samples
{

    public class Program
    {

        private const string DiscordLinkWildcard = "discord.gg/";

        public static void Main()
        {
            string content = "test123  a a https://discord.gg/2tbzHMBN";// ddgifdg s fs !";


            int inviteLinkStart = content.IndexOf(DiscordLinkWildcard, StringComparison.OrdinalIgnoreCase);
            if (inviteLinkStart < 0)
            {
                return;
            }

            string code = content.Substring(inviteLinkStart + DiscordLinkWildcard.Length);
            int spaceIndex = code.IndexOf(" ");
            if (spaceIndex > -1)
            {
                code = code.Substring(0, spaceIndex);
            }

            Console.WriteLine("'" + code + "'");

        }

    }

}