# MYTGS - Download Here
This is a simple desktop application designed for TGS student.
It is made to be a step up from my [original project](https://github.com/Torca2001/timetable-clock). 
It integrates with Firefly Education's Api to display user's tasks 
and performing caching of these. 
It will also have an inbuilt version of the timetable clock.
To install [Download](https://downloads.torca.xyz/mytgs/MYTGS.application) this.


## Firefly compatiblity 
The program is able to connect to the Firefly system and login using SSO.
This allows it to automatically retrieve relevant information such as Tasks
and planner information.


## EPR processing
For Trinity the epr follows a set format which can be parsed using regex.
This allows for the program to compare the EPR with the student's current timetable


## Timetable Clock
Similar to the [predecessor](https://github.com/Torca2001/timetable-clock) 
It will count down the time for the current period taking in account different days,
early finishes and different semesters.


## Early finishes
This is not a perfect system as it relys on an outlook Calendar from Mr Ryder which can be susceptible
to human error and thus sometimes the program can get this wrong.
This is easily resolved by going to settings and overriding the early finish for the day.
