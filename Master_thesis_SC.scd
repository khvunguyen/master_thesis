(
// waitforBoot will boot the server automatically if it is not already running
s.waitForBoot {

	// Variable 'ambisonicSound' would be an array of 2 sounds
	var ambisonicSound, decoder;

	// Define action when message is received on the address pattern "/hello"
	OSCdef(\hello,
		{
			|msg, time, replyAddr, recvPort|
			("\nMessage received on port" + recvPort + "from " + replyAddr.ip
				+ ":" + replyAddr.port + ":" + msg).postln;
	}, '/hello');

	// Define action when message is received on the address pattern "/midi"
	OSCdef(\midi,
		{
			|msg, time, replyAddr, recvPort|
			("\nMessage received on port" + recvPort + "from " + replyAddr.ip
				+ ":" + replyAddr.port + ":" + msg).postln;

			if ((msg.size() > 1) && (msg[1].isKindOf(Integer)))
			{ (midinote: msg[1]).play; }
	}, '/midi');

	// Define the decoder for ATK (binaural with HRTF from the CIPIC database)
	decoder = FoaDecoderKernel.newCIPIC;

	// Should sync often to make sure everything on the server is up-to-date
	s.sync;

	// Define a SynthDef under the symbol 'autoPiano'
	SynthDef(\autoPiano,
		{
			arg azimuth, distance;
			var sound, multipliedFactor;

			// Distance = 0 is not a valid value, set it to 0.001 in such case
			if (distance == 0) { distance = 0.001 };
			// Small distance => louder sound & vice versa
			multipliedFactor = 1.0 - distance;

			// Define an "auto" piano (1 random note per second)
			sound = MdaPiano.ar(freq: LFNoise0.kr(1).range(21, 60).round.midicps,
				gate: LFPulse.kr(1), mul: multipliedFactor);

			// Encode the signal into a non-directional soundfield
			sound = FoaEncode.ar(sound, FoaEncoderMatrix.newOmni);

			// Turn the soundfield into a planewave arriving from azimuth angle
			sound = FoaPush.ar(sound, 0.5pi, azimuth - 0.5pi);

			// Decode the soundfield
			sound = FoaDecode.ar(sound, decoder);

			// Output the sound to sound card
			Out.ar(0, sound);
	}).add;

	s.sync;

	// Create 2 new synths but in "paused" mode
	ambisonicSound = [ Synth.newPaused(\autoPiano), Synth.newPaused(\autoPiano) ];

	s.sync;

	// Define action when message is received on address patterns "/ambisonic1" & "/ambisonic2"
	2.do({ arg i;
		OSCdef(("ambisonic"++(i+1).asString).asSymbol,
			{
				|msg, time, replyAddr, recvPort|
				("\nMessage received on port" + recvPort + "from " + replyAddr.ip
					+ ":" + replyAddr.port + ":" + msg).postln;

				// Message should be an array with 3 elements
				// at index [0] is the address pattern, at [1] is distance, at [2] is azimuth
				if ((msg.size() == 3) && (msg[1].isKindOf(Float)) && (msg[2].isKindOf(Float)))
				{
					// From "paused" to "playing"
					ambisonicSound[i].run;
					// Set the value for distance and azimuth in real time
					ambisonicSound[i].set(\distance, msg[1], \azimuth, msg[2]);
				}
				{
					// When message contains less than 3 elements, pause the sound
					ambisonicSound[i].run(false);
				};
		}, ("/ambisonic"++(i+1).asString).asSymbol);
	});

	// Define action when message is received on the address pattern "/stopAll"
	OSCdef(\stopAll,
		{
			|msg, time, replyAddr, recvPort|
			("\nMessage received on port" + recvPort + "from " + replyAddr.ip
				+ ":" + replyAddr.port + ":" + msg + "\n").postln;

			// Free kernel decoder, server and all OSCDef on client (sclang)
			decoder.free; s.freeAll; OSCdef.freeAll;
			"\nEverything has been freed".postln;
	}, '/stopAll');

	s.sync;

	// Print out SC server port (user has to find out their internet IP address themselves)
	if (s.serverRunning) { ("\nSC Server is listening on port:" + NetAddr.langPort).postln; };
} // end of the function defined for s.waitForBoot
) // end of the whole code
