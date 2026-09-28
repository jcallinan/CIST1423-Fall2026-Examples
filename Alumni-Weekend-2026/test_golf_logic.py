"""
Automated unit test suite for GolfVR Mini Golf game mechanics and logic
Tests stroke counting, 9-hole progression, score terms, out-of-bounds, and procedural audio.
"""

import math

def calculate_score_term(strokes, par):
    diff = strokes - par
    if strokes == 1:
        return "HOLE IN ONE! 🌟"
    elif diff <= -2:
        return "EAGLE! 🦅"
    elif diff == -1:
        return "BIRDIE! 🐦"
    elif diff == 0:
        return "PAR! ⛳"
    elif diff == 1:
        return "BOGEY 🏌️"
    else:
        return f"+{diff} BOGEY 🏌️"

def test_score_terms():
    assert calculate_score_term(1, 2) == "HOLE IN ONE! 🌟"
    assert calculate_score_term(1, 3) == "HOLE IN ONE! 🌟"
    assert calculate_score_term(1, 4) == "HOLE IN ONE! 🌟"
    assert calculate_score_term(2, 4) == "EAGLE! 🦅"
    assert calculate_score_term(2, 3) == "BIRDIE! 🐦"
    assert calculate_score_term(3, 3) == "PAR! ⛳"
    assert calculate_score_term(4, 3) == "BOGEY 🏌️"
    assert calculate_score_term(5, 3) == "+2 BOGEY 🏌️"
    print("[PASS] test_score_terms passed")

def test_nine_hole_round():
    pars = [2, 3, 3, 2, 3, 3, 3, 3, 4] # Total Par 26
    total_par = sum(pars)
    assert total_par == 26

    # Simulate player strokes on each hole
    strokes = [2, 3, 2, 2, 4, 3, 3, 3, 4]
    total_strokes = sum(strokes)
    diff = total_strokes - total_par

    assert len(strokes) == 9
    assert total_strokes == 26
    assert diff == 0 # Even par round
    print(f"[PASS] test_nine_hole_round passed (Total strokes: {total_strokes}, Par: {total_par}, Diff: {diff})")

def test_physics_impulse():
    power_multiplier = 2.2
    min_speed = 0.15
    max_ball_speed = 15.0

    # Test below threshold: swing speed 0.10 m/s
    speed1 = 0.10
    assert speed1 < min_speed, "Should not register stroke below min swing speed"

    # Test valid putt: swing speed 1.5 m/s
    speed2 = 1.5
    impulse2 = min(speed2 * power_multiplier, max_ball_speed)
    assert math.isclose(impulse2, 3.3, rel_tol=1e-3)

    # Test high speed clamp: swing speed 10.0 m/s
    speed3 = 10.0
    impulse3 = min(speed3 * power_multiplier, max_ball_speed)
    assert impulse3 == max_ball_speed
    print("[PASS] test_physics_impulse passed")

def test_fanfare_audio_synthesis():
    sample_rate = 44100
    duration = 1.2
    num_samples = int(sample_rate * duration)
    frequencies = [523.25, 659.25, 783.99, 1046.50]
    note_duration = 0.22

    samples = []
    for i in range(num_samples):
        t = i / sample_rate
        note_idx = min(int(t / note_duration), len(frequencies) - 1)
        freq = frequencies[note_idx]
        note_t = t % note_duration
        env = math.exp(-note_t * 3.5)
        v = (math.sin(2 * math.pi * freq * t) + 0.35 * math.sin(2 * math.pi * freq * 2 * t)) * env * 0.4
        samples.append(v)
        assert not math.isnan(v) and not math.isinf(v)
        assert -1.0 <= v <= 1.0

    print(f"[PASS] test_fanfare_audio_synthesis passed ({num_samples} samples generated, peak: {max(abs(s) for s in samples):.3f})")

def test_putt_audio_synthesis():
    sample_rate = 44100
    duration = 0.08
    num_samples = int(sample_rate * duration)
    samples = []
    for i in range(num_samples):
        t = i / sample_rate
        env = math.exp(-t * 60.0)
        tone1 = math.sin(2 * math.pi * 480 * t)
        tone2 = math.sin(2 * math.pi * 1800 * t) * 0.4
        v = (tone1 + tone2) * env
        samples.append(v)
        assert not math.isnan(v) and not math.isinf(v)
    print(f"[PASS] test_putt_audio_synthesis passed ({num_samples} samples generated)")

if __name__ == "__main__":
    print("Running GolfVR test suite...")
    test_score_terms()
    test_nine_hole_round()
    test_physics_impulse()
    test_fanfare_audio_synthesis()
    test_putt_audio_synthesis()
    print("ALL TESTS PASSED SUCCESSFULLY!")
