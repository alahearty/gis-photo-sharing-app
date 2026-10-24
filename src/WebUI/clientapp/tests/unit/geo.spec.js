import { expect } from 'chai'
import {
  formatCoordinate,
  formatCoordinates,
  isValidCoordinatePair,
  isValidLatitude,
  isValidLongitude,
  openStreetMapUrl,
  toGeoJsonPosition,
  toLeafletLatLng,
  toNumber,
  validateCoordinates,
} from '@/utils/geo'

describe('coordinate utilities', () => {
  it('formats coordinates to six decimal places', () => {
    expect(formatCoordinate(4.8207794999999996)).to.equal('4.820779')
    expect(formatCoordinate(-73.9855)).to.equal('-73.985500')
    expect(formatCoordinates(4.8156, 7.0498)).to.equal('4.815600, 7.049800')
  })

  it('accepts valid latitude and longitude values', () => {
    expect(isValidLatitude(-90)).to.equal(true)
    expect(isValidLatitude(90)).to.equal(true)
    expect(isValidLongitude(-180)).to.equal(true)
    expect(isValidLongitude(180)).to.equal(true)
    expect(isValidCoordinatePair(4.8156, 7.0498)).to.equal(true)
  })

  it('rejects out-of-range and non-numeric values', () => {
    expect(isValidLatitude(91)).to.equal(false)
    expect(isValidLongitude(-181)).to.equal(false)
    expect(isValidCoordinatePair('', 7.0498)).to.equal(false)
    expect(isValidCoordinatePair('abc', 7.0498)).to.equal(false)
    expect(toNumber('   ')).to.equal(null)
    expect(toNumber('4.8156')).to.equal(4.8156)
  })

  it('returns a clear validation message', () => {
    expect(validateCoordinates(91, 7)).to.contain('Latitude')
    expect(validateCoordinates(4, 181)).to.contain('Longitude')
    expect(validateCoordinates(4.8156, 7.0498)).to.equal('')
  })

  it('creates Leaflet latitude-first positions', () => {
    expect(toLeafletLatLng(4.8156, 7.0498)).to.deep.equal([4.8156, 7.0498])
  })

  it('creates GeoJSON longitude-first positions', () => {
    expect(toGeoJsonPosition(4.8156, 7.0498)).to.deep.equal([7.0498, 4.8156])
  })

  it('does not produce a Leaflet position for invalid coordinates', () => {
    expect(toLeafletLatLng(null, 7.0498)).to.equal(null)
    expect(toGeoJsonPosition(95, 7.0498)).to.equal(null)
  })

  it('builds an OpenStreetMap link with the correct coordinates', () => {
    const url = openStreetMapUrl(4.8156, 7.0498)
    expect(url).to.contain('mlat=4.815600')
    expect(url).to.contain('mlon=7.049800')
    expect(url).to.contain('#map=16/4.815600/7.049800')
  })
})
