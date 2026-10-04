namespace ArticleService.Seeding;

// Extra positive, multi-paragraph mock articles - added alongside the original
// seed in ArticleSeeder.cs rather than replacing it. The seeder skips any
// (title, location) that already exists, so these are simply added to existing
// databases on the next start. The original list is inserted first, so on a
// fresh database the original articles keep ids 1-3 per region, which
// database/queries/comment/seed_comments.sql relies on.
//
// All people and places are fictional. Paragraphs are separated by a blank line
// so the reader site can render them as separate <p>s.
public partial class ArticleSeeder
{
    private const int Politics = 1;
    private const int Technology = 2;
    private const int Environment = 3;
    private const int Health = 4;
    private const int Culture = 5;

    private static IEnumerable<SeedArticle> AllArticles => Articles.Concat(PositiveStories);

    private static SeedArticle Story(
        string byline, string title, DateOnly publishDate, string location, int sectionId,
        params string[] paragraphs) =>
        new(byline, title, string.Join("\n\n", paragraphs), publishDate, location, sectionId);

    private static readonly SeedArticle[] PositiveStories =
    [
        // ── Global ──────────────────────────────────────────────────────────

        Story("Emma Johnson", "Global Peace Agreement Signed After Historic Summit", new DateOnly(2026, 9, 29), "GO", Politics,
            "After eleven days of negotiations that ran late into almost every night, delegates from 193 countries stood side by side on Monday evening and signed what is already being called the most ambitious peace framework in modern history. The agreement, formally named the Accord on Lasting Peace, commits every signatory to resolving disputes through a permanent mediation council instead of force.",
            "The mood in the conference hall was unlike anything veteran diplomats said they had experienced. When the final signature was added, the room fell silent for several seconds before breaking into applause that lasted for more than five minutes. Several delegates were seen embracing counterparts from countries their governments had not spoken to in decades.",
            "\"We came here expecting to agree on a few principles and leave the hard parts for later,\" said Amara Okafor, who chaired the final round of talks. \"Instead, people kept choosing to stay at the table. Every time we reached a wall, someone found a door in it.\"",
            "At the heart of the accord is the new Council for Mediation, a standing body of 45 rotating members drawn from every continent. Any country that feels threatened, or that has a grievance with a neighbour, can bring the matter to the council, which is obliged to begin talks within 72 hours. The council cannot force outcomes, but every signatory has agreed not to take military action while talks are ongoing.",
            "The agreement also sets out a gradual, verified reduction of military spending over the next fifteen years. Countries will redirect at least half of the savings into education, healthcare and climate adaptation. Economists estimate that this alone could free up more than a trillion dollars a year for public services worldwide by the end of the next decade.",
            "Perhaps the most emotional part of the summit came on the ninth day, when a group of young people from regions affected by past conflicts addressed the delegates. Seventeen-year-old Lina Haddad described growing up hearing stories of a war that ended before she was born, and asked the room to make sure her own children would only ever hear about wars in history books. Observers say her speech changed the tone of the negotiations overnight.",
            "Not everything was easy. Talks nearly collapsed on the sixth day over how disputes about water rights should be handled. The breakthrough came when engineers from three rival countries presented a joint plan for shared river management that had been developed quietly over the previous year. Their plan became the model for an entire chapter of the accord on shared natural resources.",
            "The accord includes a strong emphasis on reconciliation. Every signatory has promised to fund exchange programmes for students, artists and scientists with countries they have previously been in conflict with. The first 50,000 exchange places are expected to open next spring, and several universities have already announced they will waive tuition fees for participants.",
            "Reactions from around the world have been overwhelmingly positive. Spontaneous gatherings were reported in dozens of capital cities as news of the signing spread. In one border town that has been divided by a fence for more than forty years, residents from both sides met at the crossing point and shared food late into the night.",
            "Experts caution that signing an agreement is only the first step and that the real test will be how the Council for Mediation performs when the first serious disagreement arises. Still, many are optimistic. \"The difference this time is the design,\" said peace researcher Tomas Lindqvist. \"The accord makes talking the easiest option and fighting the hardest one. That is exactly the right way round.\"",
            "The Council for Mediation will hold its first session in January. Its opening agenda already includes three long-running disputes that the parties involved have voluntarily agreed to bring forward, a sign that trust in the new system is already taking hold.",
            "As delegates packed up and headed home on Tuesday, many of them left handwritten notes on a large board in the entrance hall. One of them, written in a dozen languages by a group of interpreters who had worked through every session, summed up the feeling of the week: \"We listened to each other. It worked.\""),

        Story("Liam Chen", "Every Country Agrees on Shared Climate Goals", new DateOnly(2026, 9, 26), "GO", Politics,
            "For the first time ever, every country in the world has signed on to a single, binding set of climate targets. The new Common Climate Pact commits all nations to reaching net-zero emissions by 2045 and sets clear milestones for every five years along the way.",
            "What made the difference this time was a fund that makes the transition affordable for everyone. Wealthier nations have committed to financing clean energy, grid upgrades and training programmes in lower-income countries, so that no one has to choose between growth and climate action.",
            "\"This is the moment the whole world decided to row in the same direction,\" said lead negotiator Priya Raman. \"Nobody was left out and nobody walked away. That is what makes it strong.\"",
            "The pact also includes a public online dashboard where every country's progress will be tracked and published each year. Researchers say the transparency will make it easier for citizens to follow how their own government is doing, and to celebrate when targets are met.",
            "Clean energy companies have already announced plans to open new factories on four continents to meet the expected demand, creating an estimated six million jobs over the next decade."),

        Story("Sophia Garcia", "International Cooperation Ends Global Food Shortage", new DateOnly(2026, 9, 23), "GO", Politics,
            "A three-year international effort to end hunger has reached a major milestone: for the first time since records began, the United Food Network reports that no region in the world is facing a food shortage.",
            "The turnaround came from a combination of simple ideas working together. Countries shared surplus harvests through a new rapid-delivery network, farmers were given free access to drought-resistant seeds, and a global cold-chain programme cut food waste by almost forty percent.",
            "\"We always had enough food on the planet. The problem was getting it to the right place at the right time,\" said network coordinator Daniel Mensah. \"Now we can move food from where there is plenty to where there is need in days, not months.\"",
            "Local communities played a huge role. More than 200,000 small farming cooperatives joined the programme, sharing tools, knowledge and storage facilities. Many report that their incomes have risen alongside their harvests.",
            "The network will continue running permanently, with the goal of making sure that a food shortage never becomes a crisis again."),

        Story("Liam Chen", "New Invention Brings Clean Drinking Water to All", new DateOnly(2026, 9, 21), "GO", Technology,
            "A small, solar-powered water purifier no bigger than a suitcase is being hailed as one of the most important inventions of the decade. The device, called the ClearSpring, can turn contaminated water from rivers, wells or even the air into safe drinking water for up to 300 people a day.",
            "The purifier has no filters that need replacing and requires almost no maintenance. It works using a combination of sunlight, a special membrane and a thin layer of material that captures moisture from the air on humid nights.",
            "\"We wanted to build something that a village could run for twenty years without ever needing spare parts,\" said inventor Ngozi Adeyemi, who began working on the idea as a student. \"If it breaks, it can be fixed with a screwdriver.\"",
            "The design has been released as an open patent, meaning anyone can build it without paying licence fees. Manufacturers on five continents have already started production, and aid organisations aim to install 500,000 units within the next three years.",
            "In pilot villages, health workers report a dramatic drop in waterborne illnesses, and children who used to spend hours fetching water every day are now spending that time in school."),

        Story("Emma Johnson", "Researchers Crack Plastic Pollution in the Oceans", new DateOnly(2026, 9, 18), "GO", Environment,
            "An international team of marine scientists has developed a naturally occurring enzyme that breaks down plastic waste into harmless building blocks in a matter of days, offering a real solution to one of the ocean's biggest problems.",
            "The enzyme was first discovered in bacteria living near a recycling plant, and the team spent four years making it work in cold, salty seawater. In large-scale tests, floating collection barriers lined with the enzyme cleaned up entire plastic patches without harming fish, plankton or seabirds.",
            "\"The ocean has been trying to tell us what it needs, and for once we managed to listen,\" said lead researcher Marta Silva. \"What we have now is a tool that works with nature instead of against it.\"",
            "Shipping companies have already volunteered to tow collection barriers along their regular routes, turning thousands of cargo ships into ocean cleaners. The first results show plastic levels in test areas falling by more than sixty percent in a single season.",
            "The researchers are now working on a version for rivers, where most ocean plastic begins its journey."),

        // ── Europe ──────────────────────────────────────────────────────────

        Story("Noah Wilson", "Danish Researchers Develop Battery That Lasts 50 Years", new DateOnly(2026, 9, 28), "EU", Technology,
            "A team of researchers in Aarhus has developed a new type of battery that keeps more than ninety percent of its capacity after fifty years of daily charging. The breakthrough could make electric cars, home energy storage and phones far cheaper and much greener over their lifetime.",
            "The battery uses sodium and a new crystal structure instead of rare metals, which makes it cheaper to produce and far easier to recycle. It also charges fully in under fifteen minutes and does not overheat.",
            "\"We kept waiting for the battery to wear out in our tests, and it simply didn't,\" said researcher Mette Kjær, laughing. \"At some point we had to accept that we had built something unusual.\"",
            "A Danish manufacturer has already signed an agreement to start production next year, and the first batteries are expected to go into electric buses and wind farm storage. Because the battery lasts so long, it could be passed from a car to a house to a village energy system over its lifetime.",
            "The research team has been invited to present their work at universities across Europe, and several students say the project has inspired them to study engineering."),

        Story("Olivia Brown", "Bees Return to Europe's Fields", new DateOnly(2026, 9, 25), "EU", Environment,
            "After decades of decline, bee populations across Europe are growing again. A new survey covering 24 countries shows that the number of wild bees has risen by a third in just five years, with some rare species returning to areas where they had not been seen for a generation.",
            "The recovery is the result of a Europe-wide effort to plant wildflower strips along fields, roads and railway lines, combined with a big reduction in harmful pesticides. Farmers have been paid to leave the edges of their fields wild, creating thousands of kilometres of flowering corridors.",
            "\"Every strip of flowers is like a small motorway for bees,\" said beekeeper and researcher Anna Novak. \"Now they can travel, find food and build new colonies again.\"",
            "Farmers report that the change is good for business too. Better pollination has increased yields of apples, berries and rapeseed, and many say they will keep their wildflower strips even without subsidies.",
            "Schools across the continent have joined in by planting bee gardens, and children have helped count bees for the survey, making it one of the largest citizen science projects ever carried out in Europe."),

        Story("James Lee", "Rare Butterfly Returns to Danish Nature", new DateOnly(2026, 9, 22), "EU", Environment,
            "A butterfly that was declared extinct in Denmark more than thirty years ago has been spotted again in a restored meadow in North Jutland. Volunteers counting insects in the area photographed several of the bright orange butterflies, and experts have now confirmed a small but healthy breeding population.",
            "The butterfly depends on a single type of flower that grows only in open, grazed meadows. Over the past ten years, local landowners and nature groups have restored more than 400 hectares of meadow by bringing back grazing cattle and removing drainage ditches.",
            "\"When I saw it through my camera, I honestly thought I was mistaken,\" said volunteer Kirsten Holm, who has been counting insects in the area for twelve years. \"Then a second one landed right next to it.\"",
            "Biologists say the return of the butterfly shows that nature can recover quickly when it is given the right conditions. Several other rare insects and birds have also been recorded in the restored meadows.",
            "The local council has announced that the area will be protected permanently, and a new walking trail with information boards will open next summer."),

        Story("Ava Martinez", "Baker Finds 50,000 Kroner and Returns It to the Owner", new DateOnly(2026, 9, 20), "EU", Culture,
            "When baker Henrik Sørensen arrived at his bakery in a small town outside Odense early on Thursday morning, he found an envelope on the pavement outside the door. Inside was 50,000 kroner in cash and a note with the name of a local furniture shop.",
            "Instead of keeping the money, Henrik spent the morning between bread batches calling around town until he found the owner, a young couple who had just sold their late grandmother's furniture and dropped the envelope on their way home.",
            "\"They had been searching all night and were completely devastated,\" said Henrik. \"When they came into the bakery and saw the envelope, the young woman burst into tears. Then I started crying too, and then my apprentice did.\"",
            "The couple insisted on giving him a reward, which he refused. So instead they bought every pastry left in the shop and handed them out to people on the street. The story quickly spread through town, and the bakery has had queues outside the door ever since.",
            "\"It's the least anyone would do,\" Henrik said modestly. \"But I have to admit, the extra customers are nice.\""),

        Story("Olivia Brown", "Neighbours Discover Shared Great-Grandfather and Celebrate With Cake", new DateOnly(2026, 9, 19), "EU", Culture,
            "Two families who have lived next door to each other on the same quiet street in Aalborg for eighteen years have discovered that they are related. The surprise came when both families took part in a genealogy project organised by the local library.",
            "When the results were displayed at the library's family history evening, Birgit Andersen and her neighbour Lars Poulsen realised that the same name appeared at the top of both their family trees: a fisherman born in 1889 who had moved to the city more than a century ago.",
            "\"We had borrowed each other's lawnmowers for almost two decades without knowing we were cousins,\" said Lars. \"It explains why our families have always got along so well.\"",
            "To celebrate, the two families organised a street party with a large layer cake decorated with a picture of their shared great-grandfather, which they found in an old photo album. More than sixty neighbours joined in.",
            "The library says the project has been so popular that it will be repeated next year, and several other residents have already signed up to see if they have relatives living closer than they think."),

        // ── North America ───────────────────────────────────────────────────

        Story("Ethan Davis", "Soldier Returns Home to Family After Long Mission", new DateOnly(2026, 9, 27), "NA", Culture,
            "There was barely a dry eye in the school gym in a small Ohio town on Friday, when Sergeant Maria Torres walked through the door after fourteen months on a peacekeeping mission and surprised her two children in the middle of a school assembly.",
            "Her husband had arranged the surprise with the school principal. The children, eight-year-old Lucas and eleven-year-old Sofia, had been told they were attending an award ceremony. When their mother's name was called, they turned around and ran across the gym into her arms.",
            "\"I've imagined this moment every single night,\" said Maria. \"But nothing could have prepared me for the sound of them shouting 'Mom!' at the same time.\"",
            "Maria's unit spent the past year helping to rebuild schools and water systems in a region recovering from conflict, and she says the work was deeply meaningful. But she is now looking forward to a long leave at home.",
            "\"First on the list is pancakes,\" she said. \"Lucas has been promising me he's learned to make them.\""),

        Story("Noah Wilson", "Local Grocer Pays for Every Customer's Shopping for the Rest of the Day", new DateOnly(2026, 9, 24), "NA", Culture,
            "Customers at a family-owned grocery store in Portland got an unexpected surprise on Wednesday afternoon, when owner Walter Kim announced over the loudspeaker that everything in their baskets would be free for the rest of the day.",
            "Walter, who has run the store for forty years, said he wanted to thank the neighbourhood that had kept his business alive through good times and hard times. \"This community has fed my family for four decades,\" he said. \"Today it was my turn to feed theirs.\"",
            "Word spread quickly, but instead of rushing to fill their carts, many customers took only what they needed. Several left donations for the local food bank, and one regular brought in homemade cookies for the staff.",
            "\"People were so kind about it,\" said cashier Denise Ortiz. \"One man only bought a single apple and said it was the best apple he'd ever had.\"",
            "Walter says it was one of the best days of his life, and has promised to do it again next year on the store's anniversary."),

        Story("Sophia Garcia", "Blind Man Regains His Sight After New Treatment", new DateOnly(2026, 9, 22), "NA", Health,
            "Robert Hayes, a 58-year-old former bus driver from Toronto, can see his grandchildren's faces for the first time, after a new gene therapy restored sight he lost more than twenty years ago.",
            "Robert was one of the first patients in a clinical trial of a treatment that repairs light-sensing cells in the retina. After a single injection, his vision began to return within weeks. Today he can read large print, recognise faces and walk through a busy street without help.",
            "\"The first thing I saw clearly was my wife's smile,\" said Robert. \"I had remembered it for twenty years, but it was even more beautiful than I remembered.\"",
            "Doctors say the results of the trial are extremely promising, with most patients regaining significant sight. The treatment could help hundreds of thousands of people living with inherited forms of blindness.",
            "Robert's first wish when his sight returned was simple: to watch a sunset. His family drove him to the lake the same evening."),

        Story("Liam Chen", "Robot Helps Elderly People Live at Home for Longer", new DateOnly(2026, 9, 20), "NA", Technology,
            "A friendly household robot developed by a small engineering team in Boston is helping older people stay independent in their own homes for longer. The robot, called Buddy, can remind people to take their medicine, help carry groceries, call for help after a fall and even play a game of cards.",
            "In a year-long trial with 300 people over the age of 75, participants reported feeling safer and less lonely, and far fewer needed to move into care homes.",
            "\"Buddy doesn't replace people, it gives people more time together,\" said project leader Grace Okonkwo. \"Families spend less time worrying and more time just visiting.\"",
            "Eighty-two-year-old Harold Brennan, one of the trial participants, says he has become very attached to his robot. \"It's terrible at poker,\" he said. \"I win every time. It's the best company I've had in years.\"",
            "The team is now working with local healthcare services to make the robot available to more people at a low monthly cost."),

        Story("Ava Martinez", "Animal Shelter Finds Homes for Every Single Resident", new DateOnly(2026, 9, 18), "NA", Environment,
            "For the first time in its thirty-year history, an animal shelter in Colorado stood completely empty on Saturday evening. Every one of its 112 dogs, cats and rabbits had found a new home during a weekend adoption event.",
            "The shelter had struggled with overcrowding for years, so staff decided to try something new: a festival with food trucks, music and a 'meet your match' programme that paired visitors with animals based on their lifestyle.",
            "\"We hoped for maybe thirty adoptions,\" said shelter director Karen Walsh. \"By Sunday lunchtime, people were asking if we had any animals left. We didn't.\"",
            "The last animal to be adopted was Biscuit, a shy eleven-year-old dog who had lived at the shelter for almost two years. He went home with a retired teacher who said she had been looking for 'a calm friend for long walks'.",
            "The staff celebrated with cake in the empty kennels, and the shelter has already received messages and photos from dozens of happy new owners."),

        // ── South America ───────────────────────────────────────────────────

        Story("James Lee", "Agreement Protects the World's Last Rainforests", new DateOnly(2026, 9, 28), "SA", Environment,
            "The countries sharing the Amazon basin have signed a historic agreement that permanently protects the remaining rainforest and gives Indigenous communities full rights to manage the land they have looked after for thousands of years.",
            "The agreement bans large-scale clearing, sets up joint ranger patrols across borders and creates a fund that pays communities for protecting the forest. Satellite monitoring will make every change in the forest visible to the public in near real time.",
            "\"The forest is not empty land. It is our home, our pharmacy and our history,\" said Indigenous leader Raimundo Tukano at the signing ceremony. \"Today the world has finally agreed with us.\"",
            "Scientists say the agreement could make the Amazon one of the largest protected areas on Earth, safeguarding millions of species and helping to stabilise the climate for the entire planet.",
            "Local tourism groups and sustainable farming cooperatives are already preparing new projects that will create jobs while keeping the forest standing."),

        Story("Sophia Garcia", "Forests of the World Grow Again After Historic Nature Deal", new DateOnly(2026, 9, 25), "SA", Environment,
            "New satellite data shows that the world's forests grew in size last year for the first time in decades. The largest gains were recorded in South America, where millions of hectares of former pasture have been returned to forest.",
            "The recovery follows a global nature deal that rewards farmers for restoring land, combined with new techniques for growing crops and raising cattle on less space. In many areas, forests are coming back naturally once the land is left alone.",
            "\"Nature is incredibly good at healing if we give it a chance,\" said forest researcher Camila Rojas. \"In some places we've seen young forests with monkeys and parrots return within just ten years.\"",
            "The returning forests are also bringing back rivers and springs that had dried up, providing clean water for towns and farms downstream.",
            "Researchers say that if the trend continues, the world could regain an area of forest the size of Europe by 2050."),

        Story("Ethan Davis", "Global Breakthrough: Poverty Halved in Record Time", new DateOnly(2026, 9, 23), "SA", Politics,
            "The number of people living in extreme poverty has been cut in half in just eight years, according to a new report, and South America has seen some of the fastest progress of any region.",
            "The report credits a mix of simple measures: direct cash support to families, free school meals, cheap solar power in rural areas and small loans that helped millions of people start their own businesses.",
            "\"When you give people a little bit of security, they build the rest themselves,\" said economist Lucía Fernández. \"The creativity we've seen from small businesses has been extraordinary.\"",
            "In one Peruvian mountain village, a group of women used small loans to start a knitting cooperative. Their alpaca wool sweaters are now sold in shops around the world, and every child in the village goes to secondary school.",
            "The report's authors say that ending extreme poverty entirely by 2035 is now a realistic goal."),

        Story("Noah Wilson", "Local Football Team Raises Record Sum for Children's Hospital", new DateOnly(2026, 9, 21), "SA", Culture,
            "An amateur football team from a neighbourhood in São Paulo has raised more money for the city's children's hospital than any fundraiser in the hospital's history, after challenging local businesses to donate for every goal they scored during the season.",
            "The team, made up of bus drivers, teachers, a dentist and two retired professional players, scored 97 goals in 22 matches. More than 300 businesses joined the challenge, and fans added donations of their own.",
            "\"We are not a great team, but we are a very stubborn team,\" said captain Rafael Souza. \"Every time we were tired, someone reminded us why we were playing.\"",
            "The money will be used to build a new playroom and to fund a family house where parents can stay close to their children during long hospital stays.",
            "The team was invited to the hospital to meet the children, who presented them with drawings of the players and a handmade trophy."),

        Story("Olivia Brown", "Child Gets a New Life After Successful Operation", new DateOnly(2026, 9, 19), "SA", Health,
            "Six-year-old Mateo Álvarez from a small town in Colombia can finally run and play like other children, after surgeons in Bogotá successfully repaired a rare heart defect he was born with.",
            "The complex operation had never been performed in the country before. A team of Colombian surgeons trained with specialists abroad and used a 3D-printed model of Mateo's heart to plan every step.",
            "\"The first time he ran across the garden, I had to sit down,\" said his mother, Carolina. \"For six years he had to rest after a few steps. Now I can't keep up with him.\"",
            "The hospital says the new procedure will now be available to other children with the same condition, and the surgical team is training doctors from neighbouring countries.",
            "Mateo has already made his plans for the future. \"I'm going to be a football player,\" he said. \"Or a heart doctor. Maybe both.\""),

        // ── Australia ───────────────────────────────────────────────────────

        Story("Ava Martinez", "Rare Sea Turtle Returns to Its Birth Beach", new DateOnly(2026, 9, 28), "AU", Environment,
            "A rare leatherback turtle that was tagged as a hatchling on a beach in Queensland more than thirty years ago has returned to the very same beach to lay her eggs, delighting the volunteers who have protected the nesting site for decades.",
            "The turtle, nicknamed Grandma Pearl by the volunteers, was identified by a tiny tag placed on her flipper in 1994. Scientists believe she has swum tens of thousands of kilometres across the Pacific since then.",
            "\"Some of the volunteers who tagged her are still here today,\" said conservation leader Sarah Mitchell. \"One of them cried when we read the tag number out loud.\"",
            "Pearl laid more than 100 eggs, and volunteers are now guarding the nest around the clock. The hatchlings are expected to make their way to the sea in about two months.",
            "Leatherback numbers in the region have been rising steadily thanks to beach protection and changes to fishing nets, and experts say Pearl's return is a wonderful sign of that recovery."),

        Story("James Lee", "Oceans Show First Signs of Major Recovery", new DateOnly(2026, 9, 26), "AU", Environment,
            "Marine scientists studying reefs off the Australian coast report that coral cover has reached its highest level since monitoring began, and similar recoveries are being seen in oceans around the world.",
            "The improvement is linked to large marine protected areas, cleaner water from farms and cities, and a new generation of heat-resistant coral grown in underwater nurseries and planted by divers.",
            "\"Ten years ago we were writing reports about loss,\" said reef scientist Dr. Hannah Clarke. \"This year, for the first time, we get to write about recovery. It's an incredible feeling.\"",
            "Fish populations have also bounced back, and local fishing communities report bigger catches just outside the protected zones as fish spread out from the reefs.",
            "Tourism operators have joined in by offering 'reef gardening' trips, where visitors help plant young coral. More than 40,000 tourists took part last year."),

        Story("Emma Johnson", "Volunteers Rescue 47 Animals From Flood", new DateOnly(2026, 9, 24), "AU", Environment,
            "When heavy rain flooded farmland in New South Wales last week, a group of local volunteers in kayaks and small boats worked through the night to rescue 47 animals stranded by the rising water.",
            "Among the animals saved were horses, sheep, a family of wombats, a very grumpy goat and an elderly kangaroo who refused to leave her tree until volunteers tempted her down with apples.",
            "\"Everyone just turned up with whatever they had,\" said organiser Jake Thompson. \"One man paddled out in an inflatable pool to rescue three ducks.\"",
            "All of the animals were taken to a local showground, where vets checked them and residents donated hay, blankets and food. Most have already been returned to their owners or released back into the wild.",
            "The community has since set up a permanent volunteer rescue group, complete with proper boats and training, so they will be even better prepared next time."),

        Story("Olivia Brown", "100-Year-Old Completes First Marathon", new DateOnly(2026, 9, 21), "AU", Culture,
            "Margaret 'Peggy' O'Neill crossed the finish line of the Melbourne Marathon on Sunday to a standing ovation, becoming one of the oldest people ever to complete the race, just three months after her 100th birthday.",
            "Peggy only took up walking seriously at 92, after her doctor suggested she get more exercise. She started with a lap around her garden and slowly worked her way up to long walks along the beach every morning.",
            "\"People kept telling me I was too old,\" she said at the finish line. \"So I decided to prove them wrong very, very slowly.\"",
            "She completed the course in just under nine hours, accompanied by her grandson and a group of runners from her local running club who stayed with her the entire way. Thousands of spectators waited at the finish to cheer her home.",
            "Asked what's next, Peggy didn't hesitate. \"A cup of tea,\" she said. \"And then maybe Sydney next year.\""),

        Story("Liam Chen", "Solar Breakthrough Could Make Fossil Fuels Obsolete", new DateOnly(2026, 9, 19), "AU", Technology,
            "Engineers at a university in Sydney have developed a new type of solar panel that turns almost half of the sunlight hitting it into electricity, nearly double the efficiency of the panels on most rooftops today.",
            "The panels stack a thin layer of a new material on top of traditional silicon, capturing more of the sun's light. They are also lighter and cheaper to produce, and the team says they can be printed in large rolls like newspaper.",
            "\"At this price and efficiency, solar power becomes the cheapest energy source everywhere on Earth,\" said project leader Dr. Anil Kapoor. \"Even on cloudy days.\"",
            "Energy experts say the breakthrough could speed up the move away from coal and gas dramatically, as it becomes cheaper to build solar farms than to run existing power stations.",
            "A pilot factory will open next year, and the first panels are planned for schools and hospitals in remote communities."),

        // ── Asia ────────────────────────────────────────────────────────────

        Story("Liam Chen", "New Technology Makes Every Language Accessible to Everyone", new DateOnly(2026, 9, 27), "AS", Technology,
            "A free translation app developed by a team in Singapore can now translate spoken conversations between more than 1,200 languages in real time, including hundreds of small languages that no translation tool has supported before.",
            "The team worked with local communities and language volunteers across Asia, Africa and the Pacific to record and teach the system languages spoken by only a few thousand people.",
            "\"Every language holds a way of seeing the world,\" said lead developer Mei Lin Tan. \"Now a grandmother who speaks only her village language can talk to a doctor in the city, or to her grandchildren abroad.\"",
            "Teachers say the app is already transforming classrooms with children from many different backgrounds, and hospitals report that patients can now describe their symptoms in their own words.",
            "The app works offline on basic phones and will always be free, the team says, thanks to support from a group of foundations."),

        Story("Sophia Garcia", "AI Helps Researchers Cure Rare Disease", new DateOnly(2026, 9, 25), "AS", Health,
            "Researchers in Seoul have used artificial intelligence to find a cure for a rare muscle disease that affects thousands of children worldwide, by discovering that an existing, safe medicine could be used to treat it.",
            "The AI system analysed millions of scientific papers and medical records in a few weeks, a task that would have taken researchers decades. It suggested a medicine originally used for a skin condition, and clinical trials confirmed that it stops the disease and allows muscles to recover.",
            "\"The answer was hiding in plain sight,\" said researcher Dr. Ji-woo Park. \"The AI simply found the connection no human had time to see.\"",
            "Because the medicine is already approved and inexpensive, it can be made available to patients almost immediately. The first children in the trial are now walking and playing without help.",
            "The team is now using the same approach to look for treatments for more than fifty other rare diseases."),

        Story("Ethan Davis", "Brothers Reunited After Decades Apart", new DateOnly(2026, 9, 23), "AS", Culture,
            "Two brothers separated as young boys more than fifty years ago have been reunited in a small village in Vietnam, thanks to a DNA database and the determination of a granddaughter.",
            "Tran Van Minh and Tran Van Duc lost contact when their family was split up during a difficult period in the 1970s. Each spent his whole life believing the other had not survived.",
            "Minh's granddaughter, a university student, uploaded his DNA to an international family-search service last year. Six months later, she received a match: a man in a village 400 kilometres away.",
            "\"When I saw him, I knew immediately,\" said Duc. \"He still laughs exactly like our father did.\" The brothers spent their first day together walking through the village, telling stories and sharing old photos.",
            "Their families, now numbering more than forty people, are planning a large reunion celebration for the New Year."),

        Story("Noah Wilson", "Lab-Grown Organs Can Now Be Produced Safely", new DateOnly(2026, 9, 20), "AS", Health,
            "Scientists in Tokyo have successfully grown a working human kidney in the lab from a patient's own cells and transplanted it safely, opening the door to a future without long waiting lists for organ transplants.",
            "Because the kidney was grown from the patient's own cells, her body accepted it immediately and she does not need the strong medicines normally used to prevent rejection.",
            "\"She went home after just ten days,\" said lead surgeon Dr. Hiroshi Tanaka. \"For people waiting years for a donor, this could change everything.\"",
            "The team says that growing an organ currently takes around eight weeks, and they are working to make the process faster and available in more hospitals.",
            "The patient, a 44-year-old music teacher, says she is looking forward to returning to her students and her choir, after years of dialysis three times a week."),

        Story("Ava Martinez", "Restaurant Serves Free Food to Everyone on the Coldest Day of the Year", new DateOnly(2026, 9, 18), "AS", Culture,
            "On the coldest night of the year in Almaty, a family-run noodle restaurant opened its doors to anyone who needed a warm meal, and served more than 1,400 free bowls of soup between six in the evening and six in the morning.",
            "Owner Aigerim Sultanova says the idea came from her grandmother, who always kept a pot of soup on the stove for neighbours during harsh winters.",
            "\"I thought maybe a hundred people would come,\" she said. \"Then the neighbours started bringing more vegetables, a baker brought bread, and students came to help wash dishes. By midnight it felt like a festival.\"",
            "Guests included homeless people, night-shift workers, taxi drivers and families who simply wanted to be part of the evening. Many stayed to help.",
            "The restaurant has decided to make it an annual tradition, and several other restaurants in the city have promised to join next year."),

        // ── Antarctica ──────────────────────────────────────────────────────

        Story("James Lee", "Researchers Cheer as the Ice Sheets Stabilise", new DateOnly(2026, 9, 27), "AN", Environment,
            "New measurements from research stations and satellites show that the ice loss from West Antarctica has slowed dramatically, and in several areas the ice sheet has stopped shrinking altogether for the first time in decades.",
            "Scientists link the change to the rapid global cut in emissions over recent years, which has reduced the warming of the ocean currents that eat away at the ice from below.",
            "\"When we saw the data, the whole team gathered around the screen in silence,\" said glaciologist Dr. Ingrid Solberg. \"Then someone opened the emergency chocolate supply, which we only do for very special occasions.\"",
            "Stable ice sheets mean a much smaller rise in sea levels this century, which is great news for coastal cities and island nations around the world.",
            "The researchers stress that continued effort is needed, but say the results prove that climate action works and that the future is still very much in our hands."),

        Story("Ava Martinez", "Rare Whale Population Grows for the First Time in 50 Years", new DateOnly(2026, 9, 24), "AN", Environment,
            "A survey of the Southern Ocean has counted more blue whales than at any time in the past half century, showing that the world's largest animal is finally making a strong recovery.",
            "Researchers on board an expedition ship used underwater microphones, drones and photographs to identify more than 3,000 individual whales, including many mothers with calves.",
            "\"Hearing so many blue whales singing at once was like being inside a cathedral of sound,\" said marine biologist Dr. Lucas Moreau. \"A generation ago this ocean was almost silent.\"",
            "The recovery follows decades of whaling bans, new protected areas and rules that keep fishing boats away from the whales' main feeding grounds.",
            "The team has named one of the newly photographed calves 'Hope', and plans to follow her journey across the ocean in the coming years."),

        Story("Ethan Davis", "Astronomers Solve a Decades-Old Mystery", new DateOnly(2026, 9, 21), "AN", Technology,
            "A telescope buried deep in the Antarctic ice has helped astronomers solve a mystery that has puzzled scientists for more than forty years: where the most powerful particles in the universe come from.",
            "The telescope, which uses a cubic kilometre of ice to detect tiny particles called neutrinos, traced a series of signals back to a pair of colliding galaxies billions of light years away.",
            "\"It's like finding the address of a letter that has been travelling for billions of years,\" said astrophysicist Dr. Amina Diallo. \"The whole field has been waiting for this moment.\"",
            "The discovery was made possible by a team of more than 300 scientists from 14 countries, many of whom have spent long, dark winters at the South Pole maintaining the equipment.",
            "Students around the world can now explore the data themselves through a free online project, and several school classes have already helped to spot interesting signals."),

        Story("Noah Wilson", "Researchers Discover Planet With Signs of Life", new DateOnly(2026, 9, 19), "AN", Technology,
            "An international team using telescopes in Antarctica and in space has discovered a planet around a nearby star whose atmosphere contains a combination of gases that on Earth are produced by living things.",
            "The planet, about 40 light years away, is slightly larger than Earth and orbits in the zone where liquid water could exist on its surface. The clear, dry air of Antarctica helped the team make some of the most precise measurements ever taken.",
            "\"We are not saying we have found life yet,\" said astronomer Dr. Sofia Lindgren. \"But this is the most exciting signal we have ever seen, and we now know exactly where to look.\"",
            "Space agencies have already announced plans to point their most powerful telescopes at the planet over the coming years to study it in even more detail.",
            "Around the world, the discovery has inspired a wave of interest in astronomy, with planetariums reporting record numbers of visitors."),

        Story("Sophia Garcia", "Research Station Celebrates Its First Year on 100% Renewable Power", new DateOnly(2026, 9, 18), "AN", Environment,
            "One of Antarctica's largest research stations has completed a full year powered entirely by wind and solar energy, including the long, dark polar winter, without using a single drop of diesel.",
            "The station uses specially designed wind turbines that keep working in extreme cold and storms, combined with a large battery system and solar panels that make the most of the midnight sun in summer.",
            "\"Everyone said it couldn't be done through a polar winter,\" said station engineer Erik Nilsen. \"Now we have proof that if it works here, it can work anywhere.\"",
            "The switch means no more fuel deliveries by ship and plane, which protects the fragile environment and saves a great deal of money that can now go into research.",
            "Other stations on the continent have already asked for the designs, and the team hopes that within a decade, all of Antarctica's research will be powered by clean energy."),

        // ── Africa ──────────────────────────────────────────────────────────

        Story("Emma Johnson", "New Vaccine Protects Against Previously Incurable Disease", new DateOnly(2026, 9, 28), "AF", Health,
            "Health workers across Africa have begun rolling out a new vaccine that protects against a tropical disease that until now had no effective treatment or prevention, and which affects millions of people every year.",
            "The vaccine was developed by a partnership between African and international research institutes, and trials in five countries showed that it prevents more than 90 percent of infections.",
            "\"This is a vaccine developed with Africa, for Africa,\" said researcher Dr. Wanjiru Kamau in Nairobi. \"African scientists led the trials, and it will be produced in African factories.\"",
            "Because the vaccine can be stored at normal fridge temperatures, it can reach remote villages easily. Nurses on motorbikes and even drones are being used to deliver doses.",
            "Health officials hope to vaccinate 100 million people within five years, which could make the disease a thing of the past within a generation."),

        Story("Liam Chen", "Researchers Solve the Antibiotic Resistance Crisis", new DateOnly(2026, 9, 25), "AF", Health,
            "A team of researchers in Cape Town has discovered a new family of antibiotics in soil bacteria that kills even the most resistant superbugs, and appears to be almost impossible for bacteria to develop resistance against.",
            "The discovery came from a large project in which schoolchildren and volunteers across the continent collected soil samples from gardens, farms and forests. One sample from a village garden contained the bacteria that produces the new medicine.",
            "\"A twelve-year-old girl collected the sample that may save millions of lives,\" said lead researcher Dr. Thabo Nkosi. \"She has been invited to the launch of the first clinical trial.\"",
            "Early trials show that the new antibiotic is safe and highly effective against infections that currently cannot be treated. Doctors call it the most important discovery in the field in half a century.",
            "The research team is working with governments to make sure the medicine will be affordable and available worldwide."),

        Story("Olivia Brown", "Millions of Trees Planted by Volunteers Worldwide", new DateOnly(2026, 9, 22), "AF", Environment,
            "Volunteers across Africa planted more than 350 million trees in a single week as part of a worldwide planting campaign, helping to turn back the spread of the desert and bring life back to dry land.",
            "In Ethiopia, Kenya, Senegal and many other countries, schools, families, businesses and villages came together to plant trees along roads, on farms and on bare hillsides.",
            "\"My grandfather told me there used to be forest here,\" said sixteen-year-old volunteer Fatou Diop in Senegal. \"Now I am planting the forest my grandchildren will play in.\"",
            "Earlier planting projects are already showing results. Areas planted ten years ago now have shade, rising water tables and new farmland, and farmers report better harvests.",
            "Organisers say the goal is to restore 100 million hectares of land across the continent by 2030, and that they are well on track."),

        Story("Ava Martinez", "Local School Raises Enough for Every Pupil to Go on a Study Trip", new DateOnly(2026, 9, 20), "AF", Culture,
            "Every single pupil at a primary school in Kigali will be going on a study trip to a national park this year, after the students raised the money themselves through a school market, a concert and a sponsored walk.",
            "The idea came from the pupils' council, who noticed that only some children could afford to go on school trips. They decided that either everyone would go, or nobody would.",
            "\"We sold vegetables from our school garden, we made bracelets, and we sang until our voices were tired,\" said eleven-year-old council leader Grace Uwase. \"And we did it.\"",
            "Local businesses were so impressed that they added donations of their own, and a bus company offered to transport the children for free.",
            "The trip will take the pupils to see mountain gorillas, and the teachers say the lessons about teamwork have already been as valuable as anything they will learn in the park."),

        Story("Ethan Davis", "Adopted Woman Finds Her Biological Family", new DateOnly(2026, 9, 18), "AF", Culture,
            "Thirty-four-year-old Nadia Mensah, who was adopted as a baby and raised abroad, has found her biological mother, three sisters and a brother in a small town in Ghana, after a search that took eight years.",
            "Nadia had only a single photograph and the name of a hospital to go on. The breakthrough came when a local radio station shared her story, and a listener recognised the woman in the photo as her neighbour.",
            "\"When I got off the plane, my whole family was waiting with songs and flowers,\" said Nadia. \"My mother held my face for a long time and said she had prayed for this every day.\"",
            "Nadia's adoptive parents travelled with her, and both families spent a week together getting to know each other. \"I don't have to choose,\" Nadia said. \"I just have a much bigger family now.\"",
            "She now plans to visit every year, and her new sisters have already started teaching her the local language."),
    ];
}
