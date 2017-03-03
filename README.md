# SmarterScheduling

## Okay, let's be honest. We all want the same thing.

You know exactly how you want your Pawns to behave. We all want roughly the same thing.

You want them to go to sleep when they are tired, and then wake up when they are rested. Easy enough.

Then you want them to go to your Dining Room / Recreation Room, to Eat and to Joy to improve their Mood for a while. This room also has great Beauty, Comfort, and Space. They love it here. Their Moods start to improve.

Then their Joy hits maximum.

But this is where it starts to go wrong.

They leave the room as soon as their Joy is full, even if their overall Mood is still low. They had great scores in Beauty, Comfort, and Space, but the deltas hadn't yet caught up, and you wanted them to stay a little longer, even after their Joy was full.

You wanted them to stay until their overall Mood had caught up with the awesome room. Only THEN did you want them to leave.

Then you want them to go Work for a while, ideally continuously until they have to Sleep. And then repeat the process.

And if, while they're out, their Joy or overall Mood hits a critically low threshold, you want them to go back to your awesome Dining / Recreation room, and stay there, *not* just until thier *Joy* is full, but until their *overall Mood* has peaked.

## The concept is simple, but this is difficult to execute in vanilla.

This is exactly what you are trying to acheive with that clunky little Scheduler, trying to slot those little Sleep, Joy, and Work blocks into that little colored grid.

But the tool just isn't up to the task. What you want is just a little too complicated, and you can't ever quite get it right.

You try to find the right balance, but it's just not the right tool for the job, and whenever your balance is a bit off, your prize Doctor goes berserk and gets his arm blown off by your Sniper.

## Enter Smarter Scheduling

This mod implements exactly that behavior we all want. Automatically.

## How It Works

First and foremost, once you download, install, and enable this mod, you cannot manually set the Time/Schedules for any Pawn. Don't bother trying. This mod is going to overwrite them.

As a matter of fact, if you open up the Time/Scheduler and watch, you can see your Pawn's Schedules update automatically from this mod.

You'll notice that this mod doesn't worry or care about the future scheduling, it continuously tells each Pawn, individually, what they should do RIGHT NOW, and as it does so, you'll see the entire Day/Schedule update to a single uniform color, and then continuously change, as needed.

Actually, it is kind of cool to watch, because you can look and see exactly which stage each Pawn is currently in.

## How to Use / What You Need to Know

The one thing the mod doesn't know is where your best Dining Room / Recreation Room is located. You need to tell this to the mod.

You'll notice the mod will immediately create an Allowed Area named "Psyche". This is for identifying the room.

If you Delete or Rename this Area, the mod will just recreate another one named "Psyche". So don't bother.

If, for some totally bizarre reason, you have an Animal Area named "Psyche", this mod will rename your Animal Area to "Psyche2", freeing up "Psyche" for its own purposes.

And if you are at your maximum Areas, and this mod cannot create "Psyche", it will probably blow up with a stack trace error or something, and might set all your muffalo on fire. I dunno. I honestly didn't test. So just make room for it.

Expand the "Psyche" Area to identify your Dining Room / Recreation Room. It should have Food, and Chairs, and sources of Joy, and good Beauty / Comfort / Space scores.

Once the Pawn is sent to Recreation / Psyche, he will be held there UNTIL HIS MOOD STOPS INCREASING **AND** UNTIL HIS MOOD IS OVER A TARGET THRESHOLD. This was the best heuristic approach I could find. It also might be a behavior that you don't like. But I couldn't think of a better overall heuristic.

## It's That Simple!

If the room is well designed and your base is well equipped and your pawns are well provisioned, you will immediately notice an enormous increase in all of your Pawn's producitivty, due to longer uninterrupted stretches of Work without needing to Sleep or Joy. You should notice a huge increase in their overall Mood, and a huge drop in frequency of Mental Breaks.

## What to Watch For

If a Pawn is having a really bad time, because their dog died, or they had to butcher their wife, or something, then instead of Mental Breaks, you'll see the mod simply adjusts automatically, holding them in Psyche more frequently, for longer periods of time, trying to keep them sane.

I think we can all agree that a Pawn staying longer in your Recreation Room is better than a Pawn that's gone Berserk.

So if you see a Pawn spending a long time in your Recreation/Psyche room, take a look at his Needs, and figure out what is wrong, and whether you can fix it.

## What This Mod Still Needs

This mod is a proof of concept. I am a backend/algorithmic coder, I am not an interface designer.

The mod has absolutely no user interface.

If you find that the overall balancing / thresholding of the mod seems to be a little off, you can easily tweak the numbers, but you need to go directly into the source code, tweak the values, and recompile the mod from source.

Also, C# is not my native language, I haven't used Visual Studio in forever, and so this entire Repository / Solution is probably a little bit malformed. Sorry.

### A Technical Explanation, and The Values You Might Want to Tweak

If you want to tweak the balancing, the values are all found at the very top of [MapComponent_Brain.cs](SmarterScheduling/SmarterScheduling/MapComponent_Brain.cs). They are:

* REST_THRESH_LOW - the threshold at which your Pawn should go to sleep. You might want to tweak this, to get them to go to bed earlier or stay awake longer.
* REST_THRESH_HIGH - a threshold that is used to determine that a Pawn is well rested, and having just woken up. I really, really don't think you want to tweak this, but maybe there's a bug here, I dunno.

* MOOD_THRESH_HIGH - after waking up or otherwise being sent to Psyche, your Pawn will remain in Psyche, at minimum, until his overall Mood is above this threshold. This is the most likely value that you'll want to tweak. Note that, in many cases, your Pawn will remain in Psyche for a bit longer than this, keep reading.

* JOY_THRESH_HIGH - similarly to REST_THRESH_HIGH, this is a threshold used to determine that a Pawn has finished Joying. You probably don't want to tweak this value. He will remain in Psyche longer than this, continue reading for the explanation.

* MOOD_THRESH_LOW - your Pawn will be prematurely sent back to Psyche if his overall Mood drops below this threshold.
* JOY_THRESH_LOW - your Pawn will be prematurely sent back to Psyche if his Joy drops below this threshold.

Your Pawn can be sent to Psyche for any of these three reasons:

1. Having just woken up from Sleep.
2. Having an overall Mood dropped below the threshold.
3. Having a Joy dropped below the threshold.

Regardless of which reason it was, the next result is the same: Your Pawn will remain in Psyche until *all three* of the following conditions are met:

1. His Joy is over the the threshold.
2. His overall Mood is over the threshold.
3. His overall Mood has stopped increasing.

## Credits

Credit is due to [FluffierThanThou](https://github.com/FluffierThanThou?tab=repositories), although he doesn't even know it.

I had absolutely no idea how to write a RimWorld mod, and whenever I needed a hint, I used his code as a reference.

You should check out his other mods! They are awesome, and I use many of them!

Credit is also due to Zorba, who also writes awesome mods. I don't know if I've read Zorba's source code, but Zorba helped keep this game fun and interesting for me, certainly long enough to write this mod.

## Call for Contributors

For the love of god, this mod needs a user interface, and the thresholds should be configurable in-game through some slider bars. I don't know how to do any of this.

Even better would be if the thresholds were configurable per-pawn. If you are willing to write the interface for this, let me know, and I will write a fork with the back-end support.
