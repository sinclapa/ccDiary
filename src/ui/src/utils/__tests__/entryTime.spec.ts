import { describe, expect, it } from 'vitest'
import { ENTRY_UTC_OFFSET_MINUTES, entryTime } from '@/utils/entryTime'

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
})
