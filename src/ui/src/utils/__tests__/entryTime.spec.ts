import { describe, expect, it } from 'vitest'
import { ENTRY_UTC_OFFSET_MINUTES, entryTime, fromEntryDate, toEntryDate } from '@/utils/entryTime'

// Node honours a change to TZ at runtime, so a test can stand in a zone ahead of or behind UTC.
function inZone (tz: string, run: () => void) {
  const was = process.env.TZ
  process.env.TZ = tz
  try {
    run()
  } finally {
    if (was === undefined) delete process.env.TZ
    else process.env.TZ = was
  }
}

describe('entryTime', () => {
  it('reads a stored time back unchanged', () => {
    expect(entryTime('1918-05-21T08:20:00Z').format('HH:mm')).toBe('08:20')
  })

  it('does not shift a time into the viewer\'s zone', () => {
    // the bug this exists to prevent: midnight rendering as 01:00 in British summer time
    expect(entryTime('1918-06-22T00:00:00Z').format('HH:mm')).toBe('00:00')
    expect(entryTime('1918-06-22T23:15:00Z').format('ddd HH:mm')).toBe('Sat 23:15')
  })

  it('keeps the date of a late entry on its own day', () => {
    expect(entryTime('1918-11-11T23:50:00Z').format('D MMMM YYYY')).toBe('11 November 1918')
  })

  it('accepts a Date as well as a string', () => {
    expect(entryTime(new Date('1919-03-05T16:00:00Z')).format('HH:mm')).toBe('16:00')
  })

  it('asks the API for the diarist\'s day, not the viewer\'s', () => {
    expect(ENTRY_UTC_OFFSET_MINUTES).toBe(0)
  })

  describe("between the viewer's clock and the diarist's", () => {
    for (const tz of ['Asia/Kolkata', 'America/New_York', 'Europe/London']) {
      it(`keeps the wall clock in ${tz}`, () => {
        inZone(tz, () => {
          const stored = new Date('1918-05-21T23:30:00Z')
          const shown = fromEntryDate(stored)
          expect([shown.getFullYear(), shown.getMonth(), shown.getDate(), shown.getHours(), shown.getMinutes()])
            .toEqual([1918, 4, 21, 23, 30])
          expect(toEntryDate(shown).toISOString()).toBe(stored.toISOString())
        })
      })
    }

    it("stores a moment picked on the viewer's clock as that clock time", () => {
      inZone('Africa/Nairobi', () => {
        expect(toEntryDate(new Date(1918, 5, 27, 7, 0)).toISOString()).toBe('1918-06-27T07:00:00.000Z')
      })
    })

    it('accepts a string', () => {
      expect(fromEntryDate('1918-11-11T11:00:00Z').getHours()).toBe(11)
    })
  })
})
