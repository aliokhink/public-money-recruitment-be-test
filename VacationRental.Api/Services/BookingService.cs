﻿using System;
using System.Collections.Generic;
using VacationRental.Api.DAL.Interfaces;
using VacationRental.Api.Models;

namespace VacationRental.Api.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IRentalRepository _rentalRepository;

        public BookingService(
            IBookingRepository bookingRepository,
            IRentalRepository rentalRepository)
        {
            _rentalRepository = rentalRepository;
            _bookingRepository = bookingRepository;
        }

        public ResourceIdViewModel AddBooking(BookingBindingModel currentBooking)
        {
            if (currentBooking.Nights <= 0)
                throw new ApplicationException("Nigts must be positive");

            if (currentBooking.Start < DateTime.Now)
                throw new ApplicationException("Booking must be in future");

            if (!_rentalRepository.HasValue(currentBooking.RentalId))
                throw new ApplicationException("Rental not found");

            var existingBookings = _bookingRepository.GetBookingsByRentalId(currentBooking.RentalId);
            var units = _rentalRepository.Get(currentBooking.RentalId).Units;

            var blockedUnits = GetAvailableUnits(existingBookings, currentBooking, units);

            if (blockedUnits >= units)
                throw new ApplicationException("Not available");

            var key = new ResourceIdViewModel { Id = _bookingRepository.Count + 1 };

            _bookingRepository.Add(key.Id, new BookingViewModel
            {
                Id = key.Id,
                Nights = currentBooking.Nights,
                RentalId = currentBooking.RentalId,
                Start = currentBooking.Start.Date,
                Unit = blockedUnits + 1,
            });

            return key;
        }

        public BookingViewModel GetBooking(int bookingId)
        {
            if (!_bookingRepository.HasValue(bookingId))
                throw new ApplicationException("Booking not found");

            return _bookingRepository.Get(bookingId);
        }

        private int GetAvailableUnits(IEnumerable<BookingViewModel> bookings, BookingBindingModel currentBooking, int rentalUnits)
        {
            int count = 0;
            foreach (var booking in bookings)
            {
                if ((currentBooking.Start < booking.Start && currentBooking.End >= booking.Start) ||
                    currentBooking.Start > booking.Start && currentBooking.Start < booking.End)
                    count++;
            }
            return count;
        }
    }
}
