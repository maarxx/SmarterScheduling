# SmarterScheduling

This is a mod for the game RimWorld by Ludeon Studios.

It automatically manages your Pawn's Sleep, Recreation, and Work cycles.

We're on Steam: https://steamcommunity.com/sharedfiles/filedetails/?id=2324536212

# Dependency

This requires another mod of mine: [ModButtons](https://github.com/maarxx/ModButtons)

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

You know exactly how you want your pawns to behave. We all want roughly the same thing.

You want them to go to sleep when they are tired, and then wake up when they are rested. Easy enough.

Then you want them to go to your Dining Room / Recreation Room, to eat and to recreation to improve their mood for a while.

Then you want them to go work for a while, ideally continuously until they have to sleep. And then repeat the process.

#### The concept is simple, but this is difficult to execute in vanilla.

This is exactly what you are trying to acheive with that clunky little Scheduler, trying to slot those little Sleep, Recreation, and Work blocks into that little colored grid.

But the tool just isn't up to the task. What you want is just a little too complicated, and you can't ever quite get it right.

You try to find the right balance, but it's just not the right tool for the job, and whenever your balance is a bit off, you either specify too much Recreation causing your pawns to waste time pinging back to your Recreation Room repeatedly, or you specify too little Recreation causing your prize doctor to go berserk and get his arm blown off by your sniper.

Whenever something happens to offset the schedule (like an emergency or combat), suddenly everybody's schedules are off, and they're trying to do the wrong thing at the wrong time. But it shouldn't be that hard to get them to do the right things!

#### Enter Smarter Scheduling

This mod implements exactly that behavior we all want. Automatically.

# Basic Explanation

You'll add the mod. You'll enable the mod. By default, it won't do anything. That's okay.

The dependency "ModButtons" will add a similarly-named tab in the bottom-right corner, with some control buttons.

You'll probably want to turn this one ON for most of the time. You might turn it OFF during real emergencies, but then you'll probably turn it back ON after the crisis has been addressed.

It will default back to OFF when you Save/Load, so remember to turn it back ON each time.

While it is ON, the mod will automatically manage each pawn's Sleep / Recreation / Work schedules. You can open the Schedule tab and watch it work.

The mod doesn't care what time it is, and doesn't worry about future scheduling -- it updates each pawn's whole schedule block to one uniform task/color, and then constantly changes it on the fly.

The mod will create an Allowable Areas for humans called "Joy".

As soon as you have a good Recreation / Dining room, which provides excellent mood scores, you'll want to update the "Joy" area to just that room. If you don't have this room yet, just leave the "Joy" area empty / undefined.

The cycle you'll see looks something like this:

* The pawn will be sent to Sleep when they are tired.
* When they wake, they will probably be hungry, so they'll be pinged to Joy for a moment to find the dining room, then released to find food. This should make them eat at the table.
* Once they're fed, the mod will put them in Recreation, and will restrict them to Joy that you've specified.
* The pawn will be held in Joy, not just until their Recreation is maximized, but until their overall Mood is maximized.
* Then it will set them to Work, and release them from Joy, back to whatever Area you had previously set.

You'll notice that with maximized Mood, that your pawn is able to work continously for a very long time, and will get lots and lots of stuff done.

Ideally, your pawn will stay out until Rest gets low, and then the mod will send them to Sleep, and then repeat the process.

If the pawn's Joy or Mood gets critically low while they are out in the field, the mod will send them back to Joy early, for another round of mood-increasing therapy.

Pawns in Joy might refuse to sit down and maximize their Comfort. I suggest keeping a workbench in the room with a filler task, and a comfortable chair, and available ingredients. I personally use stonecutting. I keep enough Beauty in my room to offset keeping some ugly stone chunks in the room. Another good option is smelting. Or rolling smokeleaf.

# Specific Additional Features

Since we are taking responsibility for controlling a pawn's Sleep, Recreation, and Work schedules, and changing their Allowed Area, we therefore inherit the responsibility to perform a couple additional tasks with this mod.

## Party

The mod knows whether there is a Party being thrown, and will set all Pawns to "Anything" for the duration of the Party.

If they are sleeping through the Party, the mod will automatically wake them up.

## Doctoring

It would be irresponsible to send all of your Doctors to Sleep or Joy while somebody needs medical treatment. Therefore, we also take responsibility of knowing whether any pawns require medical treatment, and which pawns are doctors.

While somebody needs medical treatment, all doctors are set to schedule of "Anything". If somebody is laying down and waiting to be treated, if they are still not receiving medical treatment, the mod will start selecting doctors and forcing them into "Work" until everybody gets treatment. If they are STILL not getting medical treatment, the mod will start "Resetting" your doctors, which is the equivalent of Drafting-and-Undrafting them to reset their AI and consider a new task.

As a side effect of this, you will notice that your doctor's response times have improved, and pawns overall receive medical treatment in a much quicker and snappier fashion, possibly even improving disease survival rates.

This mod also assumes that your doctors have doctoring as their highest relevant priority. If you have something as a higher priority, the mod will get confused as to why nobody is providing treatment, and start chain-resetting your doctors. This is typically indicative of an incorrect Work tab, anyway.

The mod does not explicitly force job assignments ("Prioritize"), it just sets doctor schedules and resets doctors as needed and hopes the doctors, upon being reset, do the right thing.

## Handlers and Night Owls

Since pawns using Smarter Scheduling might have a schedule that isn't exactly 24-hours anymore, it might rotate around the clock over days/weeks/years. This is usually okay, but there's a couple cases where it matters.

We look out for Animal Handlers, and if they're awake during animal sleeping hours, we just hold them in Recreation/Sleep to keep them ready to go for when the animals wake up, to give them maximum hours with the animals.

We look out for Night Owls, and do the opposite. We try to get them to sleep during the day, and if they can't, we hold them in Joy during their worst hours, so they're ready to go out for effective work the moment the debuff goes away, which hopefully syncs them up back into a good schedule for the next day.

If the pawn is both an Animal Handler and a Night Owl, the Animal Handler gets priority. Sorry dude. Daytime work for you.

## Medical Conditions Not Requiring Treatment

An interesting case is medical conditions that result in bed rest but do not require immediate treatment. This includes generally missing Health, for injuries that have already been Treated, and Treated Diseases with Immunity, such as Plague / Malaria / Flu / etc.

These conditions are very disruptive to your colony. Your pawn lays in bed, and does nothing, and furthermore disrupts other colonists to feed them. Sometimes, you want this, especially with Immunity, but sometimes, they need to get their asses out of bed every now and then, to feed themselves, or go to Joy for Recreation and Mood before they suffer a mental break in your Hospital or Dormitory.

Therefore, the mod provides two more options for how such cases should be handled:

#### Immunity Handling: "Sensitive" or "Balanced" or "Brutal".

In "Sensitive" mode, which is default behavior, any colonist who is gaining Immunity will be set to Schedule of "Anything", under which schedule they typically go rest in bed. We never drag such a colonist out of bed, regardless of Hunger or Mood.

In "Balanced" mode, we will drag a colonist out of bed to attend a Party, or to send them to Psyche for low Mood or Joy, or possibly to feed themselves, based upon the next setting described below.

In "Brutal" mode, we completely disregard their need for Immunity, and we send them to Sleep or to Joy or to Work like any other colonist.

Note that in any mode you can exempt a particular colonist by right-clicking on a bed and manually ordering priority job: "Rest Until Healed", which the mod will never interfere with.

#### Hungry Patients: "Wait to be Fed" or "Feed Themselves"

In "Wait to be Fed", which is the default behavior, any colonist resting in bed will wait for somebody else to feed them.

In "Feed Themselves" mode, if a colonist is resting in bed, and gets hungry, we will drag them out of bed, at which point they will feed themselves. They might go right back to bed after eating. Whatever.

The setting "Hungry Patients" is dependent upon setting for "Immunity Handling", when relevant. If Hungry Patients is "Feed Themselves" but Immunity Handling is "Sensitive", then patients only resting for missing health will feed themselves, but patients resting for immunity will wait to be fed.

#### Reset All Selected Pawn's Schedules to ...

This button lets you nudge a pawn's schedule into another state. If SmarterScheduling is currently turned off, it will obviously "stick". Useful to force everybody to Work for a while without SmarterScheduling at all. If SmarterScheduling is turned on, this nudge might "stick" or might get overriden back on the next cycle, based on conditions.

#### Reset All Selected Pawn's Schedule Types to ...

This one (Schedule **TYPES**) is very different, and offers two options: traditional "Work", or exceptional "MaxMood". Work is the normal behavior. "MaxMood" entirely skips Work and keeps the selected pawns exclusively in Sleep or Recreation. It is useful for maximizing moods right before an attack, or right before preparing a caravan or something. You will get an alert while colonists are in MaxMood.

# Advanced Details

#### Sleep Cycles Per Work
#### Eat Cycles Per Work
#### Joy Hold Extra

These settings are experimental edge cases and probably aren't worth using.

Sleep/Eat can be set to a double schedule where they will sleep or eat an extra time before being sent out to work, only useful if their work is very far away (think opposite corner of a very large map).

Joy Hold Extra is a latch that will hold them in Recreation a little longer if their Recreation and Mood is maximized but their Beauty / Comfort might still be increasing. Useful to squeeze out a tiny bit more mood. Questionably worthwhile.

#### Really Bad Moods, like Addiction Withdrawl

If a pawn is having a really bad time, because their dog died, or they had to butcher their wife, or something, then instead of mental breaks, you'll see the mod simply adjusts automatically, holding them in Joy more frequently, for longer periods of time, trying to keep them sane.

I think we can all agree that a pawn staying longer in your Recreation Room is better than a pawn that's gone Berserk.

So if you see a pawn spending a long time in your Recreation/Joy room, take a look at his Needs, and figure out what is wrong, and whether you can fix it.

Typically, a pawn kept in Joy will increase until their Mood has stopped increasing, which is typically a very good Mood, and then they will be released automatically.

But if a pawn is having the worst time (think addiction withdrawl), then their mood might stop increasing even while it is still very low. The mod has a minimum threshold, it will not release pawns from Joy until they are at least over their Minor Break threshold by an amount. If even your best Dining/Recreation room cannot get them past that threshold, the mod will hold them in Joy indefinitely, except to Sleep and Eat.

This is probably the behavior that you want anyway. You are certainly no worse off than removing their legs. In many cases, this behavior is powerful enough to pull a pawn through an addiction and keep both their legs intact.

Consider temporarily installing a production workbench inside Joy to keep them busy and productive, with a stockpile so that other pawns haul in the supplies they'll need.

#### Other Edge Cases I've Probably Forgotten

There's other edge cases here that we handle pretty well. If you've got any head for reading a little code, the actual implementation is pretty human-readable. You can find it over here: [the actual code routine](./SmarterScheduling/SmarterScheduling/MapComponent_SmarterScheduling.cs#L587-L808)

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

I started without any idea how to write a RimWorld mod. When I needed reference, I looked at code from other people's mods. Therefore a corresponding "Thank You!" is owed to the entire RimWorld modding community, but especially to Fluffy and Zorba and RawCode and JamesTec.
