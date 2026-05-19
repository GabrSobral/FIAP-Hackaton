"""Unit tests for the pure parsing / validation helpers in app.analyzer."""

import pytest

from app.analyzer import _extract_field, _parse, _sanitize_field, _validate_result

# ─── _sanitize_field ──────────────────────────────────────────────────────────

def test_sanitize_strips_control_characters():
    result = _sanitize_field("hello\x00world\x07end")
    assert "\x00" not in result
    assert "\x07" not in result
    assert "helloworld" in result

def test_sanitize_preserves_newline_and_tab():
    result = _sanitize_field("line1\nline2\ttabbed")
    assert "\n" in result
    assert "\t" in result

def test_sanitize_collapses_three_or_more_blank_lines():
    result = _sanitize_field("a\n\n\n\n\nb")
    assert "\n\n\n" not in result
    assert "a" in result and "b" in result

def test_sanitize_handles_empty_string():
    assert _sanitize_field("") == ""

# ─── _validate_result ─────────────────────────────────────────────────────────

_VALID = {
    "components":      "API Gateway → Application Service → PostgreSQL database with connections",
    "risks":           "Single point of failure on the gateway layer causing total service outage",
    "recommendations": "Add a load balancer and implement circuit breaker pattern for resilience",
    "feedback":        "",
}

def test_validate_passes_for_valid_input():
    _validate_result(_VALID)  # must not raise

def test_validate_raises_when_components_empty():
    with pytest.raises(ValueError, match="components"):
        _validate_result({**_VALID, "components": ""})

def test_validate_raises_when_risks_too_short():
    with pytest.raises(ValueError, match="risks"):
        _validate_result({**_VALID, "risks": "too short"})  # < 20 chars

def test_validate_raises_for_placeholder_none():
    # "none" is 4 chars, so the too-short check fires first (before placeholder check).
    with pytest.raises(ValueError, match="too short"):
        _validate_result({**_VALID, "recommendations": "none"})

def test_validate_raises_for_placeholder_na():
    # "n/a" is 3 chars, same as above.
    with pytest.raises(ValueError, match="too short"):
        _validate_result({**_VALID, "components": "n/a"})

def test_validate_raises_for_multiline_placeholder():
    # First line is a recognised placeholder; total length >= 20 so the
    # placeholder check (not the length check) is the one that fires.
    long_placeholder = "none\nThis is additional content that pushes the total past the minimum length"
    with pytest.raises(ValueError, match="placeholder"):
        _validate_result({**_VALID, "recommendations": long_placeholder})

def test_validate_does_not_require_feedback():
    _validate_result({**_VALID, "feedback": ""})  # must not raise

# ─── _parse ───────────────────────────────────────────────────────────────────

_INNER = (
    '{"components":"API Gateway routes to service layer","risks":"No redundancy on the gateway layer",'
    '"recommendations":"Add a load balancer for better overall resilience"}'
)

def test_parse_valid_json():
    result = _parse(_INNER)
    assert "API Gateway" in result["components"]
    assert "redundancy" in result["risks"]
    assert "load balancer" in result["recommendations"]

def test_parse_strips_markdown_fences():
    fenced = f"```json\n{_INNER}\n```"
    result = _parse(fenced)
    assert "API Gateway" in result["components"]

def test_parse_raises_when_required_field_is_empty():
    raw = '{"components":"","risks":"No redundancy on the gateway layer here","recommendations":"Add load balancer"}'
    with pytest.raises(ValueError):
        _parse(raw)

def test_parse_falls_back_to_regex_on_invalid_json():
    # Not valid JSON but contains the fields in key-value format.
    raw = (
        'not json but "components": "API Gateway routes to service layer of the system here", '
        '"risks": "No redundancy on the gateway layer of the entire architecture here", '
        '"recommendations": "Add a load balancer for better resilience of the overall system"'
    )
    result = _parse(raw)
    assert "API Gateway" in result["components"]

# ─── _extract_field ───────────────────────────────────────────────────────────

def test_extract_field_finds_value_in_text():
    text = '"components": "API Gateway routes traffic to backend"'
    assert "API Gateway" in _extract_field(text, "components")

def test_extract_field_returns_empty_when_not_found():
    assert _extract_field("no match here at all", "components") == ""

def test_extract_field_returns_truncated_raw_text_for_feedback_fallback():
    long_text = "x" * 500
    result = _extract_field(long_text, "feedback")
    assert len(result) == 400
