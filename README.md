# SmarterScheduling

This is a mod for the game RimWorld by Ludeon Studios.

# Table of Contents

* [Introduction](#introduction)
* [Basic Explanation](#basic-explanation)
* [Specific Additional Features](#specific-additional-features)
* [Advanced Details](#advanced-details)
* [How to Install](#how-to-install)
* [How to Update](#how-to-update)
* [Bugs, New Features, and Updates](#bugs-new-features-and-updates)
* [Contributors](#contributors)

# Introduction

#### Okay, let's be honest. We all want the same thing.

You know exactly how you want your Pawns to behave. We all want roughly the same thing.

You want them to go to sleep when they are tired, and then wake up when they are rested. Easy enough.

Then you want them to go to your Dining Room / Recreation Room, to Eat and to Joy to improve their Mood for a while. This room also has great Beauty, Comfort, and Space. They love it here. Their Moods start to improve.

Then their Joy hits maximum.

But this is where it starts to go wrong.

They leave the room as soon as their Joy is full, even if their overall Mood is still low. They had great scores in Beauty, Comfort, and Space, but the deltas hadn't yet caught up, and you wanted them to stay a little longer, even after their Joy was full.

You wanted them to stay until their overall Mood had caught up with the awesome room. Only THEN did you want them to leave.

Then you want them to go Work for a while, ideally continuously until they have to Sleep. And then repeat the process.

And if, while they're out, their Joy or overall Mood hits a critically low threshold, you want them to go back to your awesome Dining / Recreation room, and stay there, *not* just until thier *Joy* is full, but until their *overall Mood* has peaked.

#### The concept is simple, but this is difficult to execute in vanilla.

This is exactly what you are trying to acheive with that clunky little Scheduler, trying to slot those little Sleep, Joy, and Work blocks into that little colored grid.

But the tool just isn't up to the task. What you want is just a little too complicated, and you can't ever quite get it right.

You try to find the right balance, but it's just not the right tool for the job, and whenever your balance is a bit off, you either specify too much Joy causing your Pawns to waste time pinging back to your Recreation Room repeatedly, or you specify too little Joy causing your prize Doctor to go berserk and get his arm blown off by your Sniper.

#### Enter Smarter Scheduling

This mod implements exactly that behavior we all want. Automatically.

# Basic Explanation

You'll add the mod. You'll enable the mod. By default, it won't do anything. That's okay.

It might only work on new games, because it adds a Map Component, and because I really don't know.

Within the game, it adds a MainTab, probably in the far-bottom-right-corner, labeled "SmarterScheduling", with a button to turn the whole thing ON or OFF.

You'll probably want to turn this one ON for most of the time. You might turn it OFF during real emergencies, but then you'll probably turn it back ON after the crisis has been addressed.

It will default back to OFF when you Save/Load, so remember to turn it back ON each time.

While it is ON, the mod will automatically manage each pawn's Sleep / Joy / Work schedules. You can open the Restrict tab and watch it work.

The mod doesn't care what time it is, and doesn't worry about future scheduling -- it updates each pawn's whole schedule block to one uniform task/color, and then constantly changes it on the fly.

The mod will create a couple Allowable Areas: one called "Psyche" for humans, and two called "ToxicH" and "ToxicA", for humans and animals, respectively.

As soon as you have a good Recreation / Dining room, which provides excellent mood scores, you'll want to update the "Psyche" area to just that room. If you don't have this room yet, just leave the "Psyche" area empty / undefined.

The cycle you'll see looks something like this:

* The pawn will be sent to Sleep when they are tired.
* When they wake, they will probably be critically hungry, so the pawn will set them to Anything for a moment while they eat.
* Once they're fed, the pawn will set them to Joy, and will restrict them to Psyche that you've specified.
* The pawn will be held in Psyche for Joy, not just until their Joy is maximized, but until their Beauty, Comfort, Space, and overall Mood is maximized.
* Then it will set them to Work, and release them from Psyche, back to whatever Area you had previously set.

You'll notice that with maximized Mood, Beauty, Comfort, Space, Rest, and Food, that your pawn is able to work continously for a very long time, and will get lots and lots of stuff done.

Ideally, your pawn will stay out until Rest gets low, and then the mod will send them to Sleep, and then repeat the process.

If the pawn's Joy or Mood gets critically low while they are out in the field, the mod will send them back to Psyche early, for another round of mood-increasing therapy.

Pawns in Psyche might refuse to sit down and maximize their Comfort. I suggest keeping a workbench in the room with a filler task, and a comfortable chair, and available ingredients. I personally use stonecutting. I keep enough Beauty in my room to offset keeping some ugly stone chunks in the room. Another good option is smelting.

# Specific Additional Features

Since we are taking responsibility for controlling a pawn's Sleep, Joy, and Work schedules, and changing their Allowed Area, we therefore inherit the responsibility to perform a couple additional tasks with this mod.

#### Doctoring

It would be irresponsible to send all of your Doctors to Sleep or Joy while somebody needs medical treatment. Therefore, we also take responsibility of knowing whether any pawns require medical treatment, and which pawns are doctors.

While somebody needs medical treatment, all doctors are set to schedule of "Anything". If somebody is laying down and waiting to be treated, if they are still not receiving medical treatment, the mod will start selecting doctors and forcing them into "Work" until everybody gets treatment. If they are STILL not getting medical treatment, the mod will start "Resetting" your doctors, which is the equivalent of Drafting-and-Undrafting them to reset their AI and consider a new task.

As a side effect of this, you will notice that your doctor's response times have improved, and pawns overall receive medical treatment in a much quicker and snappier fashion, possibly even improving disease survival rates.

#### Party

The mod knows whether there is a Party being thrown, and will set all Pawns to "Anything" for the duration of the Party.

If they are sleeping through the Party, the mod will automatically wake them up.

#### Toxic Fallout

While we're at it, since we are taking responsibility to manage pawn's Schedules and Allowed Areas, the mod also automatically manages the tedious micromanagement required during Toxic Fallout.

You'll notice two additional Allowed Areas, "ToxicH" and "ToxicA", one for humans and one for animals, respectively.

You should update these areas to be under roofs, and they represent where pawns should go if their Toxic Buildup gets too high.

For humans, this is probably your entire base, under one large roof.

For animals, it can be a smaller section of base, or even a separate barn, or anything under a roof. You should probably make sure there's some food there.

Pawns will be restricted to these areas when their Toxic Buildup is above 35%, and they will be released when it drops below 25%.

# Advanced Details

#### Really Bad Moods, like Addiction Withdrawl

If a pawn is having a really bad time, because their dog died, or they had to butcher their wife, or something, then instead of Mental Breaks, you'll see the mod simply adjusts automatically, holding them in Psyche more frequently, for longer periods of time, trying to keep them sane.

I think we can all agree that a pawn staying longer in your Recreation Room is better than a pawn that's gone Berserk.

So if you see a pawn spending a long time in your Recreation/Psyche room, take a look at his Needs, and figure out what is wrong, and whether you can fix it.

Typically, a pawn kept in Psyche will increase until their Mood has stopped increasing, which is typically a very good Mood, and then they will be released automatically.

But if a pawn is having the worst time (think addiction withdrawl), then their mood might stop increasing even while it is still very low. The mod has a minimum threshold, it will not release pawns from Psyche until they are at least over their Minor Break threshold by 8%. If even your best Dining/Recreation room cannot get them past that threshold, the mod will hold them in Psyche indefinitely, except to Sleep and Eat.

This is probably the behavior that you want anyway. You are certainly no worse off than removing their leg. In many cases, this behavior is powerful enough to pull a pawn through an addiction and keep both their legs intact.

Consider temporarily installing a production workbench inside Psyche to keep them busy and productive, with a stockpile so that other pawns haul in the supplies they'll need.

#### Technical Summary

In summary, your Pawn can be sent to Psyche for any of these three reasons:

1. Having just woken up from a complete and restful Sleep.
2. Having an overall Mood dropped below the threshold.
3. Having a Joy dropped below the threshold.

Regardless of which reason it was, the next result is the same: Your Pawn will remain in Psyche until *all three* of the following conditions are met:

1. His Joy is over the the threshold.
2. His overall Mood is over the threshold.
3. His overall Mood has stopped increasing.

# How to Install

At the top of this page, on the right-hand-side, a little ways down, will be a green button, labeled "Clone or download". Click it, then click "Download ZIP". Your browser will download it.

Unzip it, and it will spew out a single folder, which is probably named something like `SmarterScheduling-master`.

Assuming you are working with default installation directories on a Windows system, you will want to move this entire folder into:

`C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods`

If you did it correctly, the result should be a directory structure that looks something like this:

`C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\SmarterScheduling-master\Assemblies`

Then restart RimWorld and enable it like any other mod.

# How to Update

First and foremost, please note that I never test updating mods on older saved games. You can try it, but please assume that a new game might always be necessary.

I also don't explicitly test whether the mod can be disabled on an existing game. Please also assume that a new game might always be necessary.

With that out of the way:

Updating is just deleting the previous version of the mod and then installing a new version.

So again, assuming default installation directories on a Windows system, you'll want to delete the same folder that you added during installation, which probably looks something like:

`C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\SmarterScheduling-master`

Then follow the previous instructions to download and install the new version, by repeating the same steps as installing the original version.

# Bugs, New Features, and Updates

You are currently looking at a GitHub repository for managing application code. I work out of this GitHub repository, and so to talk about bugs, new features, or updates, you need to know a little bit about navigating a GitHub repository like this one.

Beneath the aforementioned green button "Clone or download", it will say "Latest commit", followed by a couple random characters, followed by an amount of time. This stamp indicates how long ago the mod was last updated.

So if you think you found a problem, check this stamp. Perhaps the mod has already been updated since you downloaded it last, and you should download a new version and update. See the above instructions for how to update.

By default, you are probably looking at the "master" branch. You can see this at the top of the page, on the left-hand-side, a little ways down, it will say "Branch: something", probably "Branch: master", with a little down arrow.

The "master" branch contains the current version of the mod which I consider to be tested and stable. Mostly. I guess.

Most (but not all) of my mods have a "beta" branch for pre-release, which might offer new features or bug fixes that should probably work, theoretically, but I haven't really done much testing on, so I'm not quite sure.

So if you tried updating from the "master" branch, and you still think you found a bug or a problem, or if you just want to try the shiny new features before everybody else, consider downloading the "beta" branch and installing that instead.

To do this, just click the button where it says "Branch: master", and then click the option for "beta". Congratulations! You've changed branches! Follow the same steps to download and install, except instead of `SmarterScheduling-master` it will now be `SmarterScheduling-beta`. You can have both versions installed, but please don't try to have both versions enabled at once using the in-game Mod menu.

You will probably see other choices besides "master" and "beta", but I don't recommend clicking them. I am probably in the middle of working on them, and they are probably only halfway done, and broken, otherwise they'd already be part of "beta".

So if you tried updating the master branch, and you tried the beta branch, and you still thing you found a bug, or a problem, or want to suggest a new feature, wander over to the "Issues" tab. You can find this at the very top of the page, you are currently on the first tab "Code", you want to change to the second tab "Issues".

You can look here to see if your bug, issue, or suggestion is already present, and add comments if you wish.

If it's not there, look to the right-hand-side, click the green button "New issue", just type a Title, and Leave a comment, and then look below and click the green button "Submit new issue". I will get back to you. Maybe. Eventually. Meanwhile, other users might be able to chime in and help you too!

# Contributors

#### Credits

Credit is due to [FluffierThanThou](https://github.com/FluffierThanThou?tab=repositories), although he doesn't even know it.

I had absolutely no idea how to write a RimWorld mod, and whenever I needed a hint, I used his code as a reference.

You should check out his other mods! They are awesome, and I use many of them!

Credit is also due to Zorba, who also writes awesome mods. I don't know if I've read Zorba's source code, but Zorba helped fix some of the game's other shortcomings, keeping the game fun for me, certainly long enough for me to write this mod.

#### Call for Contributors

This mod needs a user interface, and the thresholds should be configurable in-game through some slider bars. I don't know how to do any of this.

Even better would be if the thresholds were configurable per-pawn. If you are willing to write the interface for this, let me know, and I will write a fork with the back-end support.
