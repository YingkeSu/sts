namespace SeedSearchPrototype;

/// <summary>
/// Version-pinned pools mirrored from SearchTheSpire v0.110.1. Keeping the
/// pool boundary in one file prevents the picker and query compiler from
/// silently falling back to a short hand-written sample list.
/// </summary>
internal static class SearchTheSpirePoolData
{
    public static readonly IReadOnlyDictionary<RunCharacter, string[]> CardPools = new Dictionary<RunCharacter, string[]>()
    {
        [RunCharacter.Ironclad] = Split("aggression|anger|armaments|ashenstrike|barricade|battletrance|bloodwall|bloodletting|bludgeon|bodyslam|brand|breakthrough|bully|burningpact|cascade|cinder|colossus|conflagration|crimsonmantle|cruelty|darkembrace|demonform|dismantle|dominate|drumofbattle|evileye|expectafight|feed|feelnopain|fiendfire|fightme|flamebarrier|forgottenritual|havoc|headbutt|hellraiser|hemokinesis|howlfrombeyond|impervious|infernalblade|inferno|inflame|ironwave|juggernaut|juggling|mangle|moltenfist|notyet|offering|onetwopunch|pactsend|perfectedstrike|pillage|pommelstrike|primalforce|pyre|rage|rampage|rupture|secondwind|setupstrike|shrugitoff|spite|stampede|stoke|stomp|stonearmor|swordboomerang|taunt|tearasunder|thrash|thunderclap|tremble|truegrit|twinstrike|unmovable|unrelenting|uppercut|vicious|whirlwind"),
        [RunCharacter.Silent] = Split("abrasive|accelerant|accuracy|acrobatics|adrenaline|afterimage|anticipate|assassinate|backflip|backstab|bladeofink|bladedance|blur|bouncingflask|bubblebubble|bullettime|burst|calculatedgamble|cloakanddagger|corrosivewave|daggerspray|daggerthrow|dash|deadlypoison|deflect|dodgeandroll|echoingslash|envenom|escapeplan|expertise|expose|fanofknives|finisher|flechettes|flickflack|sidestep|footwork|grandfinale|handtrick|haze|hiddendaggers|infiniteblades|knifetrap|leadingstrike|legsweep|malaise|masterplanner|mementomori|mirage|murder|nightmare|noxiousfumes|outbreak|phantomblades|piercingwail|pinpoint|poisonedstab|pounce|precisecut|predator|prepared|reflex|ricochet|serpentform|shadowstep|shadowmeld|skewer|slice|snakebite|speedster|stormofsteel|strangle|suckerpunch|tactician|thehunt|toolsofthetrade|tracking|untouchable|upmysleeve|welllaidplans"),
        [RunCharacter.Regent] = Split("alignment|arsenal|astralpulse|beatintoshape|begone|bigbang|blackhole|bombardment|bulwark|bundleofjoy|celestialmight|charge|childofthestars|cloakofstars|collisioncourse|comet|conqueror|convergence|cosmicindifference|crashlanding|crescentspear|crushunder|decisionsdecisions|devastate|dyingstar|foregoneconclusion|furnace|gammablast|gatherlight|genesis|glimmer|glitterstream|glow|guards|guidingstar|heavenlydrill|hegemony|heirloomhammer|hiddencache|iaminvincible|kinglykick|kinglypunch|knockoutblow|knowthyplace|lunarblast|makeitso|manifestauthority|monarchsgaze|monologue|neutronaegis|orbit|palebluedot|parry|particlewall|patter|photoncut|pillarofcreation|prophesize|quasar|radiate|refineblade|reflect|resonance|royalgamble|royalties|seekingedge|sevenstars|shiningstrike|solarstrike|spectrumshift|spoilsofbattle|stardust|summonforth|supermassive|swordsage|terraforming|thesmith|tyranny|voidform|wroughtinwar"),
        [RunCharacter.Defect] = Split("adaptivestrike|allforone|balllightning|barrage|beamcell|boostaway|bootsequence|buffer|bulkup|capacitor|chaos|chargebattery|chill|claw|coldsnap|compact|compiledriver|consumingshadow|coolant|coolheaded|creativeai|darkness|defragment|doubleenergy|echoform|feral|fightthrough|flakcannon|focusedstrike|ftl|fusion|geneticalgorithm|glacier|glasswork|gofortheeyes|gunkup|hailstorm|helixdrill|hologram|hotfix|hyperbeam|icelance|iteration|leap|lightningrod|loop|machinelearning|meteorstrike|modded|momentumstrike|multicast|null|overclock|rainbow|reboot|refract|rocketpunch|scavenge|scrape|shadowshield|shatter|signalboost|skim|smokestack|spinner|storm|subroutine|sunder|supercritical|sweepingbeam|synchronize|synthesis|tempest|teslacoil|thunder|trashtotreasure|turbo|uproar|voltaic|whitenoise"),
        [RunCharacter.Necrobinder] = Split("afterlife|bansheescry|blightstrike|boneshards|borrowedtime|bury|calcify|callofthevoid|capturespirit|cleanse|countdown|dansemacabre|deathmarch|deathbringer|deathsdoor|debilitate|defile|defy|delay|demesne|devourlife|dirge|drainpower|dredge|eidolon|endofdays|enfeeblingtouch|eradicate|fear|fetch|flatten|friendship|gravewarden|graveblast|hang|haunt|highfive|invoke|lethality|melancholy|misery|necromastery|negativepulse|neurosurge|noescape|oblivion|pagestorm|parse|poke|pullaggro|pullfrombelow|putrefy|rattle|reanimate|reap|reaperform|reave|righthandhand|sacrifice|scourge|sculptingstrike|seance|sentrymode|severance|sharedfate|shroud|sicem|sleightofflesh|snap|soulstorm|sow|spiritofash|spur|squeeze|thescythe|timesup|transfigure|undeath|veilpiercer|wisp"),
    };

    public static readonly IReadOnlyDictionary<RunCharacter, string[]> RareCards = new Dictionary<RunCharacter, string[]>()
    {
        [RunCharacter.Ironclad] = Split("aggression|barricade|brand|cascade|conflagration|crimsonmantle|darkembrace|demonform|dominate|feed|fiendfire|hellraiser|impervious|juggernaut|mangle|notyet|offering|onetwopunch|pactsend|primalforce|pyre|stoke|tearasunder|thrash|unmovable"),
        [RunCharacter.Silent] = Split("abrasive|adrenaline|afterimage|assassinate|bladeofink|bullettime|burst|corrosivewave|envenom|fanofknives|grandfinale|knifetrap|malaise|masterplanner|murder|nightmare|outbreak|serpentform|shadowstep|shadowmeld|stormofsteel|thehunt|toolsofthetrade|tracking|welllaidplans"),
        [RunCharacter.Regent] = Split("arsenal|beatintoshape|bigbang|bombardment|bundleofjoy|comet|crashlanding|decisionsdecisions|dyingstar|foregoneconclusion|genesis|guards|heavenlydrill|heirloomhammer|iaminvincible|makeitso|monarchsgaze|neutronaegis|royalties|seekingedge|sevenstars|swordsage|thesmith|tyranny|voidform"),
        [RunCharacter.Defect] = Split("adaptivestrike|allforone|buffer|consumingshadow|coolant|creativeai|defragment|echoform|flakcannon|geneticalgorithm|helixdrill|hyperbeam|icelance|machinelearning|meteorstrike|modded|multicast|rainbow|reboot|shatter|signalboost|spinner|supercritical|trashtotreasure|voltaic"),
        [RunCharacter.Necrobinder] = Split("bansheescry|callofthevoid|demesne|devourlife|eidolon|endofdays|eradicate|hang|misery|necromastery|neurosurge|oblivion|reanimate|reaperform|sacrifice|seance|sentrymode|sharedfate|soulstorm|spiritofash|squeeze|thescythe|timesup|transfigure|undeath"),
    };

    public static readonly IReadOnlyDictionary<RunCharacter, string[]> CharacterCapsuleRelics = new Dictionary<RunCharacter, string[]>()
    {
        [RunCharacter.Ironclad] = Split("charonsashes|demontongue|paperphrog|redskull|ruinedhelmet|selfformingclay"),
        [RunCharacter.Silent] = Split("helicaldart|paperkrane|sneckoskull|tingsha|toughbandages|twistedfunnel"),
        [RunCharacter.Regent] = Split("fencingmanual|galacticdust|lunarpastry|miniregent|orangedough|regalite"),
        [RunCharacter.Defect] = Split("datadisk|emotionchip|goldplatedcables|powercell|metronome|symbioticvirus"),
        [RunCharacter.Necrobinder] = Split("bighat|boneflute|bookrepairknife|bookmark|funerarymask|ivorytile"),
    };

    public static readonly IReadOnlyDictionary<RunCharacter, string[]> CharacterShopRelics = new Dictionary<RunCharacter, string[]>()
    {
        [RunCharacter.Ironclad] = Split("brimstone"),
        [RunCharacter.Silent] = Split("ninjascroll"),
        [RunCharacter.Regent] = Split("vitruvianminion"),
        [RunCharacter.Defect] = Split("runiccapacitor"),
        [RunCharacter.Necrobinder] = Split("undyingsigil"),
    };

    public static readonly string[] SharedCapsuleRelics = Split("akabeko|amethystaubergine|anchor|artofwar|bagofmarbles|bagofpreparation|beatingremnant|bellows|bloodvial|bookoffiverings|bowlerhat|bronzescales|candelabra|captainswheel|centennialpuzzle|chandelier|cloakclasp|eternalfeather|festivepopper|frozenegg|gamblingchip|gamepiece|girya|gorget|gremlinhorn|happyflower|horncleat|icecream|intimidatinghelmet|josspaper|juzubracelet|kunai|kusarigama|lantern|lastingcandy|letteropener|lizardtail|luckyfysh|mango|mealticket|meatonthebone|mercuryhourglass|miniaturecannon|moltenegg|mummifiedhand|nunchaku|oddlysmoothstone|oldcoin|orichalcum|ornamentalfan|pantograph|parryingshield|pear|pennib|pendulum|permafrost|petrifiedtoad|planisphere|pocketwatch|potionbelt|prayerwheel|rainbowring|razortooth|redmask|regalpillow|reptiletrinket|ripplebasin|shovel|shuriken|sparklingrouge|stonecalendar|stonecracker|strawberry|strikedummy|sturdyclamp|thecourier|tinymailbox|toxicegg|tungstenrod|tuningfork|unceasingtop|unsettlinglamp|vajra|vambrace|venerableteaset|vexingpuzzlebox|warpaint|whetstone|whitebeaststatue|whitestar");
    public static readonly string[] SharedShopRelics = Split("beltbuckle|bread|burningsticks|cauldron|chemicalx|dingyrug|dollysmirror|dragonfruit|ghostseed|gnarledhammer|kifuda|lavalamp|leeswaffle|membershipcard|miniaturetent|mysticlighter|orrery|punchdagger|ringingtriangle|royalstamp|screamingflagon|slingofcourage|theabacus|toolbox|wingcharm");
    public static readonly string[] SharedPotions = Split("attackpotion|beetlejuice|blessingoftheforge|blockpotion|bottledpotential|clarity|colorlesspotion|cureall|dexteritypotion|distilledchaos|dropletofprecognition|duplicator|energypotion|entropicbrew|explosiveampoule|fairyinabottle|firepotion|flexpotion|fortifier|fruitjuice|fyshoil|gamblersbrew|gigantificationpotion|heartofiron|liquidbronze|liquidmemories|luckytonic|mazalethsgift|orobicacid|potionofbinding|powdereddemise|powerpotion|radianttincture|regenpotion|shacklingpotion|shipinabottle|skillpotion|sneckooil|speedpotion|stableserum|strengthpotion|swiftpotion|touchofinsanity|vulnerablepotion|weakpotion");

    public static readonly IReadOnlyDictionary<RunCharacter, string[]> CharacterPotions = new Dictionary<RunCharacter, string[]>()
    {
        [RunCharacter.Ironclad] = Split("bloodpotion|soldiersstew|ashwater"),
        [RunCharacter.Silent] = Split("poisonpotion|ghostinajar|cunningpotion"),
        [RunCharacter.Regent] = Split("starpotion|cosmicconcoction|kingscourage"),
        [RunCharacter.Defect] = Split("focuspotion|essenceofdarkness|potionofcapacity"),
        [RunCharacter.Necrobinder] = Split("potionofdoom|potofghouls|bonebrew"),
    };

    public static readonly IReadOnlyDictionary<RunCharacter, string[]> CommonCards = new Dictionary<RunCharacter, string[]>()
    {
        [RunCharacter.Ironclad] = Split("anger|armaments|bloodwall|bodyslam|breakthrough|cinder|havoc|headbutt|ironwave|moltenfist|perfectedstrike|pommelstrike|setupstrike|shrugitoff|swordboomerang|taunt|thunderclap|tremble|truegrit|twinstrike"),
        [RunCharacter.Silent] = Split("anticipate|backflip|bladedance|cloakanddagger|daggerspray|daggerthrow|deadlypoison|deflect|dodgeandroll|flickflack|leadingstrike|piercingwail|poisonedstab|predator|prepared|ricochet|slice|snakebite|suckerpunch|untouchable"),
        [RunCharacter.Regent] = Split("astralpulse|begone|celestialmight|cloakofstars|collisioncourse|cosmicindifference|crescentspear|crushunder|gatherlight|glitterstream|glow|guidingstar|hiddencache|knowthyplace|patter|photoncut|refineblade|solarstrike|spoilsofbattle|wroughtinwar"),
        [RunCharacter.Defect] = Split("balllightning|barrage|beamcell|boostaway|chargebattery|claw|coldsnap|compiledriver|coolheaded|focusedstrike|gofortheeyes|gunkup|hologram|hotfix|leap|lightningrod|momentumstrike|sweepingbeam|turbo|uproar"),
        [RunCharacter.Necrobinder] = Split("afterlife|blightstrike|defile|defy|drainpower|fear|flatten|gravewarden|graveblast|invoke|negativepulse|poke|pullaggro|reap|reave|scourge|sculptingstrike|snap|sow|wisp"),
    };

    public static readonly IReadOnlyDictionary<RunCharacter, string[]> UncommonCards = new Dictionary<RunCharacter, string[]>()
    {
        [RunCharacter.Ironclad] = Split("ashenstrike|battletrance|bloodletting|bludgeon|bully|burningpact|colossus|cruelty|dismantle|drumofbattle|evileye|expectafight|feelnopain|fightme|flamebarrier|forgottenritual|hemokinesis|howlfrombeyond|infernalblade|inferno|inflame|juggling|pillage|rage|rampage|rupture|secondwind|spite|stampede|stomp|stonearmor|unrelenting|uppercut|vicious|whirlwind"),
        [RunCharacter.Silent] = Split("accelerant|accuracy|acrobatics|backstab|blur|bouncingflask|bubblebubble|calculatedgamble|dash|echoingslash|escapeplan|expertise|expose|finisher|flechettes|sidestep|footwork|handtrick|haze|hiddendaggers|infiniteblades|legsweep|mementomori|mirage|noxiousfumes|phantomblades|pinpoint|pounce|precisecut|reflex|skewer|speedster|strangle|tactician|upmysleeve"),
        [RunCharacter.Regent] = Split("alignment|blackhole|bulwark|charge|childofthestars|conqueror|convergence|devastate|furnace|gammablast|glimmer|hegemony|kinglykick|kinglypunch|knockoutblow|lunarblast|manifestauthority|monologue|orbit|palebluedot|parry|particlewall|pillarofcreation|prophesize|quasar|radiate|reflect|resonance|royalgamble|shiningstrike|spectrumshift|stardust|summonforth|supermassive|terraforming"),
        [RunCharacter.Defect] = Split("bootsequence|bulkup|capacitor|chaos|chill|compact|darkness|doubleenergy|feral|fightthrough|ftl|fusion|glacier|glasswork|hailstorm|iteration|loop|null|overclock|refract|rocketpunch|scavenge|scrape|shadowshield|skim|smokestack|storm|subroutine|sunder|synchronize|synthesis|tempest|teslacoil|thunder|whitenoise"),
        [RunCharacter.Necrobinder] = Split("boneshards|borrowedtime|bury|calcify|capturespirit|cleanse|countdown|dansemacabre|deathmarch|deathbringer|deathsdoor|debilitate|delay|dirge|dredge|enfeeblingtouch|fetch|friendship|haunt|highfive|lethality|melancholy|noescape|pagestorm|parse|pullfrombelow|putrefy|rattle|righthandhand|severance|shroud|sicem|sleightofflesh|spur|veilpiercer"),
    };

    public static readonly IReadOnlyDictionary<string, string> RelicRarity = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["akabeko"] = "Uncommon",
        ["amethystaubergine"] = "Common",
        ["anchor"] = "Common",
        ["artofwar"] = "Rare",
        ["bagofmarbles"] = "Common",
        ["bagofpreparation"] = "Common",
        ["beatingremnant"] = "Rare",
        ["bellows"] = "Rare",
        ["bloodvial"] = "Common",
        ["bookoffiverings"] = "Common",
        ["bowlerhat"] = "Uncommon",
        ["bronzescales"] = "Common",
        ["candelabra"] = "Uncommon",
        ["captainswheel"] = "Rare",
        ["centennialpuzzle"] = "Common",
        ["chandelier"] = "Rare",
        ["cloakclasp"] = "Rare",
        ["eternalfeather"] = "Uncommon",
        ["festivepopper"] = "Common",
        ["frozenegg"] = "Rare",
        ["gamblingchip"] = "Rare",
        ["gamepiece"] = "Rare",
        ["girya"] = "Rare",
        ["gorget"] = "Common",
        ["gremlinhorn"] = "Uncommon",
        ["happyflower"] = "Common",
        ["horncleat"] = "Uncommon",
        ["icecream"] = "Rare",
        ["intimidatinghelmet"] = "Rare",
        ["josspaper"] = "Uncommon",
        ["juzubracelet"] = "Common",
        ["kunai"] = "Rare",
        ["kusarigama"] = "Uncommon",
        ["lantern"] = "Common",
        ["lastingcandy"] = "Uncommon",
        ["letteropener"] = "Uncommon",
        ["lizardtail"] = "Rare",
        ["luckyfysh"] = "Uncommon",
        ["mango"] = "Rare",
        ["mealticket"] = "Common",
        ["meatonthebone"] = "Rare",
        ["mercuryhourglass"] = "Uncommon",
        ["miniaturecannon"] = "Uncommon",
        ["moltenegg"] = "Rare",
        ["mummifiedhand"] = "Rare",
        ["nunchaku"] = "Uncommon",
        ["oddlysmoothstone"] = "Common",
        ["oldcoin"] = "Rare",
        ["orichalcum"] = "Uncommon",
        ["ornamentalfan"] = "Uncommon",
        ["pantograph"] = "Uncommon",
        ["parryingshield"] = "Uncommon",
        ["pear"] = "Uncommon",
        ["pennib"] = "Uncommon",
        ["pendulum"] = "Common",
        ["permafrost"] = "Uncommon",
        ["petrifiedtoad"] = "Uncommon",
        ["planisphere"] = "Uncommon",
        ["pocketwatch"] = "Rare",
        ["potionbelt"] = "Common",
        ["prayerwheel"] = "Rare",
        ["rainbowring"] = "Rare",
        ["razortooth"] = "Rare",
        ["redmask"] = "Common",
        ["regalpillow"] = "Common",
        ["reptiletrinket"] = "Uncommon",
        ["ripplebasin"] = "Uncommon",
        ["shovel"] = "Rare",
        ["shuriken"] = "Rare",
        ["sparklingrouge"] = "Uncommon",
        ["stonecalendar"] = "Rare",
        ["stonecracker"] = "Uncommon",
        ["strawberry"] = "Common",
        ["strikedummy"] = "Common",
        ["sturdyclamp"] = "Rare",
        ["thecourier"] = "Rare",
        ["tinymailbox"] = "Uncommon",
        ["toxicegg"] = "Rare",
        ["tungstenrod"] = "Rare",
        ["tuningfork"] = "Uncommon",
        ["unceasingtop"] = "Rare",
        ["unsettlinglamp"] = "Rare",
        ["vajra"] = "Common",
        ["vambrace"] = "Uncommon",
        ["venerableteaset"] = "Common",
        ["vexingpuzzlebox"] = "Rare",
        ["warpaint"] = "Common",
        ["whetstone"] = "Common",
        ["whitebeaststatue"] = "Rare",
        ["whitestar"] = "Rare",
        ["charonsashes"] = "Rare",
        ["demontongue"] = "Rare",
        ["paperphrog"] = "Uncommon",
        ["redskull"] = "Common",
        ["ruinedhelmet"] = "Rare",
        ["selfformingclay"] = "Uncommon",
        ["helicaldart"] = "Rare",
        ["paperkrane"] = "Rare",
        ["sneckoskull"] = "Common",
        ["tingsha"] = "Uncommon",
        ["toughbandages"] = "Rare",
        ["twistedfunnel"] = "Uncommon",
        ["datadisk"] = "Common",
        ["emotionchip"] = "Rare",
        ["goldplatedcables"] = "Uncommon",
        ["powercell"] = "Rare",
        ["metronome"] = "Rare",
        ["symbioticvirus"] = "Uncommon",
        ["bighat"] = "Rare",
        ["boneflute"] = "Common",
        ["bookrepairknife"] = "Uncommon",
        ["bookmark"] = "Rare",
        ["funerarymask"] = "Uncommon",
        ["ivorytile"] = "Rare",
        ["fencingmanual"] = "Common",
        ["galacticdust"] = "Uncommon",
        ["lunarpastry"] = "Rare",
        ["miniregent"] = "Rare",
        ["orangedough"] = "Rare",
        ["regalite"] = "Uncommon",
    };

    public static string[] Split(string value) => value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
