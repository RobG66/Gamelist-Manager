﻿using System.Collections.Generic;

namespace GamelistManager
{
    public class SystemIdResolver
    {
        private Dictionary<string, int> systemValuesDictionary;

        public SystemIdResolver()
        {
            systemValuesDictionary = new Dictionary<string, int>
            {
                { "3do", 29 },
                { "3ds", 17 },
                { "abuse", 0 },
                { "adam", 89 },
                { "advision", 78 },
                { "amiga1200", 64 },
                { "amiga500", 64 },
                { "amigacd32", 130 },
                { "amigacdtv", 129 },
                { "amstradcpc", 65 },
                { "apfm1000", 0 },
                { "apple2", 86 },
                { "apple2gs", 217 },
                { "arcadia", 94 },
                { "archimedes", 84 },
                { "arduboy", 263 },
                { "astrocde", 44 },
                { "atari2600", 26 },
                { "atari5200", 40 },
                { "atari7800", 41 },
                { "atari800", 43 },
                { "atarist", 42 },
                { "atom", 36 },
                { "atomiswave", 53 },
                { "bbc", 37 },
                { "boom3", 0 },
                { "c128", 66 },
                { "c20", 73 },
                { "c64", 66 },
                { "camplynx", 88 },
                { "cannonball", 0 },
                { "cave3rd", 0 },
                { "cavestory", 135 },
                { "cdi", 133 },
                { "cdogs", 0 },
                { "cgenius", 0 },
                { "channelf", 80 },
                { "coco", 144 },
                { "colecovision", 48 },
                { "corsixth", 0 },
                { "cplus4", 99 },
                { "crvision", 241 },
                { "daphne", 49 },
                { "devilutionx", 0 },
                { "dos", 0 },
                { "dreamcast", 23 },
                { "easyrpg", 231 },
                { "ecwolf", 0 },
                { "eduke32", 0 },
                { "electron", 85 },
                { "fbneo", 75 },
                { "fds", 106 },
                { "flash", 0 },
                { "flatpak", 0 },
                { "fm7", 97 },
                { "fmtowns", 253 },
                { "fpinball", 199 },
                { "fury", 0 },
                { "gamate", 266 },
                { "gameandwatch", 52 },
                { "gamecom", 121 },
                { "gamecube", 13 },
                { "gamegear", 21 },
                { "gamepock", 95 },
                { "gb", 9 },
                { "gb2players", 0 },
                { "gba", 12 },
                { "gbc", 10 },
                { "gbc2players", 0 },
                { "gmaster", 103 },
                { "gp32", 101 },
                { "gx4000", 0 },
                { "gzdoom", 0 },
                { "hcl", 0 },
                { "hurrican", 0 },
                { "ikemen", 0 },
                { "intellivision", 115 },
                { "jaguar", 27 },
                { "laser310", 0 },
                { "lcdgames", 75 },
                { "lowresnx", 0 },
                { "lutro", 206 },
                { "lynx", 28 },
                { "macintosh", 146 },
                { "mame", 75 },
                { "mastersystem", 2 },
                { "megadrive", 1 },
                { "megaduck", 90 },
                { "model2", 0 },
                { "model3", 55 },
                { "moonlight", 138 },
                { "mrboom", 0 },
                { "msu - md", 0 },
                { "msx1", 113 },
                { "msx2", 116 },
                { "msx2 +", 117 },
                { "msxturbor", 118 },
                { "mugen", 0 },
                { "multivision", 0 },
                { "n64", 14 },
                { "n64dd", 122 },
                { "namco2x6", 0 },
                { "naomi", 56 },
                { "naomi2", 56 },
                { "nds", 15 },
                { "neogeo", 142 },
                { "neogeocd", 70 },
                { "nes", 3 },
                { "ngp", 25 },
                { "ngpc", 82 },
                { "o2em", 0 },
                { "odcommander", 0 },
                { "openbor", 214 },
                { "openlara", 0 },
                { "pc88", 221 },
                { "pc98", 208 },
                { "pcengine", 31 },
                { "pcenginecd", 114 },
                { "pcfx", 72 },
                { "pdp1", 0 },
                { "pet", 240 },
                { "pico", 250 },
                { "pico8", 234 },
                { "plugnplay", 0 },
                { "pokemini", 211 },
                { "ports", 0 },
                { "prboom", 0 },
                { "ps2", 58 },
                { "ps3", 59 },
                { "psp", 61 },
                { "psvita", 62 },
                { "psx", 57 },
                { "pv1000", 74 },
                { "pygame", 0 },
                { "pyxel", 0 },
                { "quake3", 0 },
                { "raze", 0 },
                { "reminiscence", 0 },
                { "samcoupe", 213 },
                { "satellaview", 107 },
                { "saturn", 22 },
                { "scummvm", 123 },
                { "scv", 67 },
                { "sdlpop", 0 },
                { "sega32x", 19 },
                { "segacd", 20 },
                { "sg1000", 109 },
                { "sgb", 127 },
                { "snes", 4 },
                { "snes - msu1", 210 },
                { "socrates", 0 },
                { "solarus", 223 },
                { "sonicretro", 0 },
                { "spectravideo", 218 },
                { "steam", 0 },
                { "sufami", 108 },
                { "superbroswar", 0 },
                { "supergrafx", 105 },
                { "supervision", 207 },
                { "supracan", 100 },
                { "thextech", 0 },
                { "thomson", 141 },
                { "ti99", 205 },
                { "tic80", 222 },
                { "triforce", 0 },
                { "tutor", 0 },
                { "tyrian", 0 },
                { "tyrquake", 0 },
                { "uzebox", 216 },
                { "vc4000", 0 },
                { "vectrex", 102 },
                { "vgmplay", 0 },
                { "videopacplus", 0 },
                { "virtualboy", 11 },
                { "vis", 144 },
                { "vitaquake2", 0 },
                { "vpinball", 198 },
                { "vsmile", 120 },
                { "wasm4", 262 },
                { "wii", 16 },
                { "wiiu", 18 },
                { "windows", 138 },
                { "windows_installers", 138 },
                { "wswan", 45 },
                { "wswanc", 46 },
                { "x1", 220 },
                { "x68000", 79 },
                { "xash3d_fwgs", 0 },
                { "xbox", 32 },
                { "xbox360", 33 },
                { "xegs", 0 },
                { "xrick", 0 },
                { "zc210", 0 },
                { "zx81", 77 },
                { "zxspectrum", 76 }
            };
        }


        public int ResolveSystemId(string systemKey)
        {
            if (systemValuesDictionary.TryGetValue(systemKey.ToLower(), out int systemId))
            {
                return systemId;
            }

            return 0;
        }
    }
}